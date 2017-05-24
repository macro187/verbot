using System;
using System.Diagnostics;
using MacroExceptions;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MacroIO;
using MacroConsole;

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


}
}
