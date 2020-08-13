using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MacroExceptions;
using Verbot.Refs;

namespace Verbot.Commands
{
    class RepairCommand : ICommand
    {

        readonly Context Context;
        bool WasOnMasterBranch;


        public RepairCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");

            while (true)
            {
                var failure = Context.CheckContext.Check();
                if (failure == null) return 0;

                if (failure.Repair == null)
                {
                    Trace.TraceError(failure.Description);
                    Trace.TraceInformation(failure.RepairDescription);
                    return 1;
                }

                Trace.TraceInformation(failure.Description);
                Trace.TraceInformation(failure.RepairDescription);

                ReleaseBranch();
                failure.Repair();
                Context.ResetContexts();
                RestoreBranch();
            }
        }


        void ReleaseBranch()
        {
            var branch = Context.RefContext.Head.SymbolicTarget;
            if (branch == null) return;
            if (!(branch is MasterBranchInfo)) return;
            WasOnMasterBranch = true;
            Context.GitRepository.Checkout(branch.TargetSha1);
        }


        void RestoreBranch()
        {
            if (!WasOnMasterBranch) return;
            var head = Context.RefContext.Head;
            var branch =
                Context.RefContext.Branches
                    .Where(b => b.Target == head.Target)
                    .OfType<MasterBranchInfo>()
                    .FirstOrDefault();
            if (branch == null) return;
            Context.GitRepository.Checkout(branch.Name);
            WasOnMasterBranch = false;
        }

    }
}
