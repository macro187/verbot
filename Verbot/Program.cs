using System;
using System.Diagnostics;
using MacroExceptions;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MacroIO;
using MacroConsole;
using MacroGit;
using System.Linq;

namespace Verbot
{
    class Program
    {

        static Context Context;


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
            var gitRepository = GitRepository.FindContainingRepository(Environment.CurrentDirectory);
            if (gitRepository == null) throw new UserException("Not in a Git repository");

            var verbose = false;
            while(args.Any() && args.Peek().StartsWith("--"))
            {
                var option = args.Dequeue();
                switch (option)
                {
                    case "--verbose":
                        verbose = true;
                        break;
                    default:
                        throw new UserException($"Unrecognised option '{option}'");
                }
            }

            Context = new Context(gitRepository, verbose);

            if (args.Count == 0) throw new UserException("Expected <command>");
            var command = args.Dequeue();

            return command.ToLowerInvariant() switch
            {
                "help" => Help(args),
                "calc" => Calc(args),
                "write" => Write(args),
                "reset" => Reset(args),
                "read" => Read(args),
                "release" => Release(args),
                "push" => Push(args),
                "check" => Check(args),
                "check-remote" => CheckRemote(args),
                _ => throw new UserException("Unrecognised command: " + command),
            };
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
            var release = false;
            var prerelease = false;
            while (args.Count > 0)
            {
                var arg = args.Dequeue();
                switch (arg)
                {
                    case "--release":
                        release = true;
                        break;
                    case "--prerelease":
                        prerelease = true;
                        break;
                    default:
                        throw new UserException("Unrecognised argument: " + arg);
                }
            }

            if (release && prerelease)
            {
                throw new UserException("Can't calculate --release and --prerelease version at the same time");
            }

            var calculatedInfo = Context.CalculationContext.Calculate(Context.RefContext.Head.Target);

            var version =
                release
                    ? calculatedInfo.CalculatedReleaseVersion
                : prerelease
                    ? calculatedInfo.CalculatedPrereleaseVersion
                : calculatedInfo.Version;

            Console.Out.WriteLine(version);
            return 0;
        }


        static int Write(Queue<string> args)
        {
            var release = false;
            var prerelease = false;
            while (args.Count > 0)
            {
                var arg = args.Dequeue();
                switch (arg)
                {
                    case "--release":
                        release = true;
                        break;
                    case "--prerelease":
                        prerelease = true;
                        break;
                    default:
                        throw new UserException("Unrecognised argument: " + arg);
                }
            }

            if (release && prerelease)
            {
                throw new UserException("Can't calculate --release and --prerelease version at the same time");
            }

            var version =
                release
                    ? new WriteCommand(Context).WriteReleaseVersion()
                : prerelease
                    ? new WriteCommand(Context).WritePrereleaseVersion()
                    : new WriteCommand(Context).WriteVersion();

            Console.Out.WriteLine(version.ToString());
            return 0;
        }


        static int Reset(Queue<string> args)
        {
            if (args.Count > 0)
            {
                throw new UserException("Unexpected arguments");
            }

            new WriteCommand(Context).WriteDefaultVersion();
            return 0;
        }


        static int Read(Queue<string> args)
        {
            if (args.Count > 0)
            {
                throw new UserException("Unexpected arguments");
            }

            var version = new ReadCommand(Context).ReadVersion();
            Console.Out.WriteLine(version);
            return 0;
        }


        static int Release(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");
            Context.ReleaseContext.Release();
            return 0;
        }


        static int Push(Queue<string> args)
        {
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
            new OldCommands(Context).Push(dryRun);

            return 0;
        }


        static int Check(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");
            Context.CheckContext.CheckLocal();
            return 0;
        }


        static int CheckRemote(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");
            new OldCommands(Context).CheckRemote();
            return 0;
        }

    }
}
