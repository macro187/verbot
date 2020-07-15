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

        readonly RefInfo Ref;


        public MasterBranchInfo(RefInfo @ref, SemVersion series)
        {
            Guard.NotNull(@ref, nameof(@ref));

            Guard.NotNull(series, nameof(series));
            if (series.Patch != 0 || series.Prerelease != "" || series.Build != "")
            {
                throw new ArgumentException("Not a minor version", nameof(series));
            }

            Ref = @ref;
            Series = series;
        }


        public SemVersion Series { get; }
        public GitRefNameComponent Name => Ref.Name;
        public GitFullRefName FullName => Ref.FullName;
        public bool IsBranch => Ref.IsBranch;
        public bool IsTag => Ref.IsTag;
        public GitSha1 TargetSha1 => Ref.TargetSha1;
        public CommitInfo Target => Ref.Target;

    }
}
