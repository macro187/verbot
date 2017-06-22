using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MacroExceptions;


namespace
verbot
{


internal static class
AssemblyInfoHelper
{


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
