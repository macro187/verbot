using System;
using System.Diagnostics;
using MacroExceptions;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MacroIO;
using MacroConsole;
using MacroGit;
using MacroSemver;

namespace Verbot
{
    class Program
    {

        static int Main(string[] args)
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


        static int Main2(Queue<string> args)
        {
            if (args.Count == 0) throw new UserException("Expected <command>");
            var command = args.Dequeue();

            switch (command.ToLowerInvariant())
            {
                case "help":
                    return Help(args);
                case "calc":
                    return Calc(args);
                case "get":
                    return Get(args);
                case "set":
                    return Set(args);
                case "increment":
                    return Increment(args);
                case "release":
                    return Release(args);
                case "push":
                    return Push(args);
                case "check":
                    return Check(args);
                case "check-remote":
                    return CheckRemote(args);
                default:
                    throw new UserException("Unrecognised command: " + command);
            }
        }


        static int Help(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");

            Trace.TraceInformation("");
            using (var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("Verbot.readme.md"))
            using (var reader = new StreamReader(stream))
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


        static int Calc(Queue<string> args)
        {
            var repository = GetCurrentRepository();

            var verbose = false;
            while (args.Count > 0)
            {
                var arg = args.Dequeue();
                switch (arg)
                {
                    case "--verbose":
                        verbose = true;
                        break;
                    default:
                        throw new UserException("Unrecognised argument: " + arg);
                }
            }

            var version = repository.Calc(verbose);

            Console.Out.WriteLine(version.ToString());
            return 0;
        }


        static int Get(Queue<string> args)
        {
            var repository = GetCurrentRepository();

            if (args.Count > 0) throw new UserException("Unexpected arguments");

            var version = repository.GetVersion();
            Console.Out.WriteLine(version.ToString());
            return 0;
        }


        static int Set(Queue<string> args)
        {
            var repository = GetCurrentRepository();

            SemVersion version;
            if (args.Count == 0) throw new UserException("Expected <version>");
            if (!SemVersion.TryParse(args.Dequeue(), out version))
                throw new UserException("Expected <version> in semver format");

            if (args.Count > 0) throw new UserException("Unexpected arguments");

            repository.SetVersion(version);
            return 0;
        }


        static int Increment(Queue<string> args)
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

            repository.IncrementVersion(major, minor);
            return 0;
        }


        static int Release(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");
            var repository = GetCurrentRepository();
            repository.Release();
            return 0;
        }


        static int Push(Queue<string> args)
        {
            var repository = GetCurrentRepository();

            bool dryRun = false;
            while (args.Count > 0)
            {
                var arg = args.Dequeue();
                switch (arg)
                {
                    case "--dry-run":
                        dryRun = true;
                        break;
                    default:
                        throw new UserException($"Unrecognised argument: {arg}");
                }
            }
            repository.Push(dryRun);

            return 0;
        }


        static int Check(Queue<string> args)
        {
            var repository = GetCurrentRepository();

            if (args.Count > 0) throw new UserException("Unexpected arguments");

            repository.CheckLocal();

            return 0;
        }


        static int CheckRemote(Queue<string> args)
        {
            var repository = GetCurrentRepository();

            if (args.Count > 0) throw new UserException("Unexpected arguments");

            repository.CheckRemote();

            return 0;
        }


        static VerbotRepository GetCurrentRepository()
        {
            var repo = GitRepository.FindContainingRepository(Environment.CurrentDirectory);
            if (repo == null) throw new UserException("Not in a Git repository");
            return new VerbotRepository(repo.Path);
        }

    }
}
