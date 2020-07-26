using System;
using MacroGit;
using MacroGuards;
using MacroSemver;
using Verbot.Commits;
using Verbot.Refs;

namespace Verbot.MasterBranches
{
    class MasterBranchInfo
    {

        readonly BranchInfo Branch;


        public MasterBranchInfo(BranchInfo branch, SemVersion series)
        {
            Guard.NotNull(branch, nameof(branch));

            Guard.NotNull(series, nameof(series));
            if (series.Patch != 0 || series.Prerelease != "" || series.Build != "")
            {
                throw new ArgumentException("Not a minor version", nameof(series));
            }

            Branch = branch;
            Series = series;
        }


        public SemVersion Series { get; }
        public GitRefNameComponent Name => Branch.Name;
        public GitFullRefName FullName => Branch.FullName;
        public GitSha1 TargetSha1 => Branch.TargetSha1;
        public CommitInfo Target => Branch.Target;

    }
}
