using System;
using System.Collections.Generic;
using MacroExceptions;

namespace Verbot.Commands
{
    class WriteCommand : ICommand
    {

        readonly Context Context;


        public WriteCommand(Context context)
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

            var version =
                release
                    ? Context.CalculationContext.Calculate(Context.RefContext.Head.Target).CalculatedReleaseVersion
                : prerelease
                    ? Context.CalculationContext.Calculate(Context.RefContext.Head.Target).CalculatedPrereleaseVersion
                    : Context.CalculationContext.Calculate(Context.RefContext.Head.Target).Version;

            Context.DiskLocationContext.WriteVersion(version);
            Console.Out.WriteLine(version);

            return 0;
        }

    }
}
