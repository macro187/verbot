using System;
using System.Diagnostics;
using MacroExceptions;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MacroSystem;
using MacroIO;
using MacroConsole;
using MacroGit;
using MacroSln;
using System.Linq;
using System.Text.RegularExpressions;
using Semver;


namespace
verbot
{


class
Program
{


static int
Main(string[] args)
{
    Trace.Listeners.Add(new ConsoleApplicationTraceListener());

    try
    {
        return Main2(new Queue<string>(args));
    }
    catch (UserException ue)
    {
        Trace.TraceError(ue.Message);
        return 1;
    }
    catch (Exception e)
    {
        Trace.TraceError("Internal verbot error");
        Trace.TraceError(ExceptionExtensions.Format(e));
        return 1;
    }
}


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Globalization",
    "CA1308:NormalizeStringsToUppercase",
    Justification = "Commands are spelled in lowercase")]
static int
Main2(Queue<string> args)
{
    if (args.Count == 0) throw new UserException("Expected <command>");
    var command = args.Dequeue();

    switch (command.ToLowerInvariant())
    {
        case "help":
            return Help(args);
        case "get":
            return Get(args);
        case "set":
            return Set(args);
        default:
            throw new UserException("Unrecognised command: " + command);
    }
}


static int
Help(Queue<string> args)
{
    if (args.Count > 0) throw new UserException("Unexpected arguments");

    Trace.TraceInformation("");
    using (var reader = new StreamReader(Assembly.GetCallingAssembly().GetManifestResourceStream("verbot.readme.md")))
    {
        foreach (
            var line
            in ReadmeFilter.SelectSections(
                reader.ReadAllLines(),
                "Synopsis",
                "Description",
                "Commands"
                ))
        {
            Trace.TraceInformation(line);
        }
    }

    return 0;
}


static int
Get(Queue<string> args)
{
    var repository = GetRepository();
    var sln = GetSolution(repository);

    if (args.Count > 0) throw new UserException("Unexpected arguments");

    var assemblyInfos = FindAssemblyInfos(sln);
    if (assemblyInfos.Count == 0)
        throw new UserException("No AssemblyInfo files found in solution");

    var assemblyInfoVersions = FindAssemblyInfoVersions(assemblyInfos);
    if (assemblyInfoVersions.Count == 0)
        throw new UserException("No [AssemblyInformationalVersion] attributes found in solution");

    var distinctVersions =
        assemblyInfoVersions
            .Select(aiv => aiv.Version)
            .Distinct()
            .ToList();

    if (distinctVersions.Count > 1)
    {
        Trace.TraceInformation("[AssemblyInformationalVersion] attributes in solution");
        foreach (var aiv in assemblyInfoVersions)
        {
            Trace.TraceInformation("  {0} in {1}", aiv.Version, aiv.Path);
        }
        throw new UserException("Conflicting [AssemblyInformationalVersion] attributes encountered");
    }

    Console.Out.WriteLine(distinctVersions.Single());
    return 0;
}


static int
Set(Queue<string> args)
{
    var repository = GetRepository();
    var sln = GetSolution(repository);

    SemVersion version;
    if (args.Count == 0) throw new UserException("Expected <version>");
    if (!SemVersion.TryParse(args.Dequeue(), out version))
        throw new UserException("Expected <version> in semver format");

    if (args.Count > 0) throw new UserException("Unexpected arguments");

    var assemblyVersion = FormattableString.Invariant(
        $"{version.Major}.0.0.0");

    var assemblyFileVersion = FormattableString.Invariant(
        $"{version.Major}.{version.Minor}.{version.Patch}.0");

    var assemblyInfos = FindAssemblyInfos(sln);
    if (assemblyInfos.Count == 0)
        throw new UserException("No AssemblyInfo files found in solution");

    int count = 0;
    foreach (var assemblyInfo in assemblyInfos)
    {
        if (TrySetAssemblyAttributeValue(assemblyInfo, "AssemblyInformationalVersion", version.ToString())) count++;
    }

    if (count == 0)
        throw new UserException("No [AssemblyInformationalVersion] attributes found in AssemblyInfo files");

    foreach (var assemblyInfo in assemblyInfos)
    {
        TrySetAssemblyAttributeValue(assemblyInfo, "AssemblyVersion", assemblyVersion);
        TrySetAssemblyAttributeValue(assemblyInfo, "AssemblyFileVersion", assemblyFileVersion);
    }

    return 0;
}


static GitRepository
GetRepository()
{
    var repo = GitRepository.FindContainingRepository(Environment.CurrentDirectory);
    if (repo == null) throw new UserException("Not in a Git repository");
    return repo;
}


static VisualStudioSolution
GetSolution(GitRepository repository)
{
    var sln = VisualStudioSolution.Find(repository.Path);
    if (sln == null) throw new UserException("No Visual Studio solution found");
    return sln;
}


static IList<string>
FindAssemblyInfos(VisualStudioSolution sln)
{
    return
        // All projects
        sln.ProjectReferences
            // ...that are C# projects
            .Where(pr => pr.TypeId == VisualStudioProjectTypeIds.CSharp)
            // ..."local" to the solution
            .Where(pr => !pr.Location.StartsWith("..", StringComparison.Ordinal))
            .Select(pr => pr.GetProject())
            // ...all their compile items
            .SelectMany(p =>
                p.CompileItems.Select(path =>
                    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(p.Path), path))))
            // ...that are .cs files
            .Where(path => Path.GetExtension(path) == ".cs")
            // ...and have "AssemblyInfo" in their name
            .Where(path => Path.GetFileNameWithoutExtension(path).Contains("AssemblyInfo"))
            .Distinct()
            .ToList();
}


static IList<AssemblyInfoVersion>
FindAssemblyInfoVersions(IEnumerable<string> assemblyInfos)
{
    return
        assemblyInfos
            .Select(path => new {
                Path = path,
                Version = FindAssemblyAttributeValue(path, "AssemblyInformationalVersion") })
            .Where(aiv => aiv.Version != null)
            .Select(aiv => {
                SemVersion version;
                if (!SemVersion.TryParse(aiv.Version, out version))
                    throw new UserException(StringExtensions.FormatInvariant(
                        "[AssemblyInformationalVersion] in {0} doesn't contain a valid semver",
                        aiv.Path));
                return new AssemblyInfoVersion(aiv.Path, version);
            })
            .ToList();
}


static string
FindAssemblyAttributeValue(string path, string attribute)
{
    foreach (var line in File.ReadLines(path))
    {
        var match = Regex.Match(line, "^\\s*\\[assembly: " + attribute + "\\(\"([^\"]+)\"\\)\\]\\s*$");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
    }
    return null;
}


static bool
TrySetAssemblyAttributeValue(string path, string attribute, string value)
{
    var result = new List<string>();

    var lineNumber = 0;
    var found = false;
    foreach (var line in File.ReadLines(path))
    {
        lineNumber++;

        var match = Regex.Match(line, "^(\\s*)\\[assembly: " + attribute + "\\(\"[^\"]+\"\\)\\]\\s*$");
        if (!match.Success)
        {
            result.Add(line);
            continue;
        }

        if (found)
            throw new UserException(new TextFileParseException(
                FormattableString.Invariant($"Multiple {attribute} attributes in AssemblyInfo file"),
                path, lineNumber, line));

        var indent = match.Groups[1].Value;

        result.Add(FormattableString.Invariant($"{indent}[assembly: {attribute}(\"{value}\")]"));

        found = true;
    }

    if (found) File.WriteAllLines(path, result);
    return found;
}


class
AssemblyInfoVersion
{
    public
    AssemblyInfoVersion(string path, SemVersion version)
    {
        Path = path;
        Version = version;
    }
    public string Path { get; }
    public SemVersion Version { get; }
}


}
}
