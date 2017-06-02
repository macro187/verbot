using System;
using System.Diagnostics;
using MacroExceptions;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MacroIO;
using MacroConsole;
using MacroGit;
using MacroSln;
using System.Linq;
using System.Text.RegularExpressions;

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
        default:
            throw new UserException("Unrecognised command: " + command);
    }
}


static int
Help(Queue<string> args)
{
    if (args.Count > 0) throw new UserException("Unexpected arguments");

    using (var reader = new StreamReader(Assembly.GetCallingAssembly().GetManifestResourceStream("verbot.readme.md")))
    {
        foreach (
            var line
            in ReadmeFilter.SelectSections(
                reader.ReadAllLines(),
                "Synopsis",
                "Commands"))
        {
            Trace.TraceInformation(line);
        }
    }

    return 0;
}


static int
Get(Queue<string> args)
{
    var repo = GitRepository.FindContainingRepository(Environment.CurrentDirectory);
    if (repo == null) throw new UserException("Not in a Git repository");

    var sln = VisualStudioSolution.Find(repo.Path);
    if (sln == null) throw new UserException("No Visual Studio solution found");

    var assemblyInfos =
        // All projects
        sln.ProjectReferences
            // ...that are C# projects
            .Where(pr => pr.TypeId == VisualStudioProjectTypeIds.CSharp)
            // ..."local" to the solution
            .Where(pr => !pr.Location.StartsWith(".."))
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
            .OrderBy(path => path)
            .ToList();

    if (assemblyInfos.Count == 0)
        throw new UserException("No `AssemblyInfo` files found in solution");

    var assemblyInfoVersions =
        assemblyInfos
            .Select(path => new {
                Path = path,
                Version = FindAssemblyAttributeValue(path, "AssemblyInformationalVersion") })
            .Where(aiv => aiv.Version != null)
            .ToList();

    if (assemblyInfoVersions.Count == 0)
        throw new UserException("No [AssemblyInformationalVersion] attributes found in solution");

    var distinctVersions =
        assemblyInfoVersions
            .Select(aiv => aiv.Version)
            .Distinct()
            .ToList();

    if (distinctVersions.Count > 1)
    {
        Trace.TraceInformation("[AssemblyInformationalVersion]s in solution");
        foreach (var aiv in assemblyInfoVersions)
        {
            Trace.TraceInformation("  {0} in {1}", aiv.Version, aiv.Path);
        }
        throw new UserException("Conflicting [AssemblyInformationalVersion]s found in solution");
    }

    Console.Out.WriteLine(distinctVersions.Single());
    return 0;
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


}
}
