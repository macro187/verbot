using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MacroExceptions;
using MacroGuards;
using MacroSemver;


namespace
verbot
{


internal class
AssemblyInfoFile
{


public
AssemblyInfoFile(string path)
{
    Guard.NotNull(path, nameof(path));
    Path = path;
}


public string
Path
{
    get;
}


public SemVersion
FindVersion()
{
    var aiv = FindAssemblyAttributeValue("AssemblyInformationalVersion");
    if (string.IsNullOrWhiteSpace(aiv)) return null;
    SemVersion version;
    if (!SemVersion.TryParse(aiv, out version))
        throw new UserException(FormattableString.Invariant(
            $"[AssemblyInformationalVersion] in {Path} doesn't contain a valid semver"));
    return version;
}


public string
FindAssemblyAttributeValue(string attribute)
{
    foreach (var line in File.ReadLines(Path))
    {
        var match = Regex.Match(line, "^\\s*\\[assembly: " + attribute + "\\(\"([^\"]+)\"\\)\\]\\s*$");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
    }
    return null;
}


public bool
TrySetAssemblyAttributeValue(string attribute, string value)
{
    var result = new List<string>();

    var lineNumber = 0;
    var found = false;
    foreach (var line in File.ReadLines(Path))
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
                Path, lineNumber, line));

        var indent = match.Groups[1].Value;

        result.Add(FormattableString.Invariant($"{indent}[assembly: {attribute}(\"{value}\")]"));

        found = true;
    }

    if (found) File.WriteAllLines(Path, result);
    return found;
}


}
}
