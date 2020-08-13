using System.Collections.Generic;
using System.Diagnostics;
using MacroExceptions;
using MacroGit;

namespace Verbot.Commands
{
    class ReleaseCommand : ICommand
    {

        readonly Context Context;


        public ReleaseCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");

            if (new CheckCommand(Context).Run(new Queue<string>()) != 0) return 1;

            var version = Context.CalculationContext.Calculate(Context.RefContext.Head.Target).CalculatedReleaseVersion;
            var release = Context.ReleaseContext.FindRelease(version);
            if (release != null) throw new UserException($"Version {version} already released");

            Trace.TraceInformation($"Tagging {version}");
            Context.GitRepository.CreateTag(new GitRefNameComponent(version));
            Context.ResetContexts();

            return new RepairCommand(Context).Run(new Queue<string>());
        }

    }
}
