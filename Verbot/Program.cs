using System;
using System.Diagnostics;
using MacroExceptions;
using System.Collections.Generic;
using MacroConsole;
using MacroGit;
using System.Linq;
using Verbot.Commands;

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

            if (args.Count == 0) throw new UserException("Expected <command>");
            var commandName = args.Dequeue();

            var context = new Context(gitRepository, verbose);

            ICommand command = commandName.ToLowerInvariant() switch
            {
                "help" => new HelpCommand(),
                "calc" => new CalcCommand(context),
                "write" => new WriteCommand(context),
                "reset" => new ResetCommand(context),
                "read" => new ReadCommand(context),
                "release" => new ReleaseCommand(context),
                "push" => new PushCommand(context),
                "check" => new CheckCommand(context),
                "check-remote" => new CheckRemoteCommand(context),
                "repair" => new RepairCommand(context),
                _ => throw new UserException("Unrecognised command: " + commandName),
            };

            return command.Run(args);
        }

    }
}
