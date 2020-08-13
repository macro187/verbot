using System;
using System.Collections.Generic;
using MacroExceptions;

namespace Verbot.Commands
{
    class CalcCommand : ICommand
    {

        readonly Context Context;


        public CalcCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
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

            var state = Context.CalculationContext.Calculate(Context.RefContext.Head.Target);

            var version =
                release
                    ? state.CalculatedReleaseVersion
                : prerelease
                    ? state.CalculatedPrereleaseVersion
                : state.Version;

            Console.Out.WriteLine(version);

            return 0;
        }

    }
}
