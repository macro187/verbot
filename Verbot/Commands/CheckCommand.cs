using System.Collections.Generic;
using System.Diagnostics;
using MacroExceptions;

namespace Verbot.Commands
{
    class CheckCommand : ICommand
    {

        readonly Context Context;


        public CheckCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");

            var failure =
                Context.CheckContext.CheckNoMergeCommits() ??
                Context.CheckContext.CheckNoReleaseZero() ??
                Context.CheckContext.CheckNoCommitsWithMultipleReleases() ??
                Context.CheckContext.CheckNoMissingMajorReleases() ??
                Context.CheckContext.CheckNoMissingMinorReleases() ??
                Context.CheckContext.CheckNoMissingPatchReleases() ??
                Context.CheckContext.CheckReleaseOrdering() ??
                Context.CheckContext.CheckMajorReleaseOrdering() ??
                Context.CheckContext.CheckMinorReleaseOrdering() ??
                Context.CheckContext.CheckPatchReleaseOrdering() ??
                Context.CheckContext.CheckMajorReleaseSemverChanges() ??
                Context.CheckContext.CheckMinorReleaseSemverChanges() ??
                Context.CheckContext.CheckPatchReleaseSemverChanges() ??
                Context.CheckContext.CheckNoMissingLatestBranches() ??
                Context.CheckContext.CheckLatestBranchesAtCorrectReleases() ??
                Context.CheckContext.CheckNoMissingMasterBranches() ??
                Context.CheckContext.CheckMasterBranchesInCorrectPlaces() ??
                null;

            if (failure != null)
            {
                Trace.TraceError(failure.Description);
                Trace.TraceInformation(failure.RepairDescription);
                return 1;
            }

            return 0;
        }

    }
}
