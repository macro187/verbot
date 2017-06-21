using System;
using System.Diagnostics;
using MacroExceptions;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MacroIO;
using MacroConsole;
using MacroGit;
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
        case "increment":
            return Increment(args);
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
    var repository = GetCurrentRepository();

    if (args.Count > 0) throw new UserException("Unexpected arguments");

    var version = GetCommand.Get(repository);
    Console.Out.WriteLine(version.ToString());
    return 0;
}


static int
Set(Queue<string> args)
{
    var repository = GetCurrentRepository();

    SemVersion version;
    if (args.Count == 0) throw new UserException("Expected <version>");
    if (!SemVersion.TryParse(args.Dequeue(), out version))
        throw new UserException("Expected <version> in semver format");

    if (args.Count > 0) throw new UserException("Unexpected arguments");

    SetCommand.Set(repository, version);
    return 0;
}


static int
Increment(Queue<string> args)
{
    var repository = GetCurrentRepository();

    bool major = false;
    bool minor = false;
    while (args.Count > 0)
    {
        var arg = args.Dequeue();
        switch (arg)
        {
            case "--major":
                major = true;
                minor = false;
                break;
            case "--minor":
                major = false;
                minor = true;
                break;
            case "--patch":
                major = false;
                minor = false;
                break;
            default:
                throw new UserException("Unrecognised argument: " + arg);
        }
    }

    IncrementCommand.Increment(repository, major, minor);
    return 0;
}


static VerbotRepository
GetCurrentRepository()
{
    var repo = GitRepository.FindContainingRepository(Environment.CurrentDirectory);
    if (repo == null) throw new UserException("Not in a Git repository");
    return new VerbotRepository(repo.Path);
}


}
}
