using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using IOPath = System.IO.Path;
using System.Text.RegularExpressions;
using MacroExceptions;
using MacroGit;
using MacroSln;


namespace
verbot
{


public class
VerbotRepository
    : GitRepository
{


public
VerbotRepository(string path)
    : base(path)
{
    var sln = VisualStudioSolution.Find(Path);
    if (sln == null) throw new UserException("No Visual Studio solution found in repository");
    Solution = sln;
}


public VisualStudioSolution
Solution
{
    get;
}


public IList<string>
FindAssemblyInfos()
{
    return
        // All projects
        Solution.ProjectReferences
            // ...that are C# projects
            .Where(pr => pr.TypeId == VisualStudioProjectTypeIds.CSharp)
            // ..."local" to the solution
            .Where(pr => !pr.Location.StartsWith("..", StringComparison.Ordinal))
            .Select(pr => pr.GetProject())
            // ...all their compile items
            .SelectMany(p =>
                p.CompileItems.Select(path =>
                    IOPath.GetFullPath(IOPath.Combine(IOPath.GetDirectoryName(p.Path), path))))
            // ...that are .cs files
            .Where(path => IOPath.GetExtension(path) == ".cs")
            // ...and have "AssemblyInfo" in their name
            .Where(path => IOPath.GetFileNameWithoutExtension(path).Contains("AssemblyInfo"))
            .Distinct()
            .ToList();
}


public static string
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


public static bool
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


}
}
