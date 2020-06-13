using System;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    class MasterBranchInfo
    {

        public MasterBranchInfo(RefInfo @ref, SemVersion series)
        {
            Guard.NotNull(@ref, nameof(@ref));
            Guard.NotNull(series, nameof(series));
            if (series.Patch != 0 || series.Prerelease != "" || series.Build != "")
            {
                throw new ArgumentException("Not a minor version", nameof(series));
            }

            Name = @ref.Name;
            FullName = @ref.FullName;
            Target = @ref.Target;
            Series = series;
        }


        public GitRefNameComponent Name { get; }
        public GitFullRefName FullName { get; }
        public CommitInfo Target { get; }
        public bool IsBranch { get; }
        public bool IsTag { get; }
        public SemVersion Series { get; }

    }
}
