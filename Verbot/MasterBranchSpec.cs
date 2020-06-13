using System;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    class MasterBranchSpec
    {

        public MasterBranchSpec(SemVersion series, CommitInfo commit, GitRefNameComponent name)
        {
            Guard.NotNull(series, nameof(series));
            if (series.Patch != 0 || series.Prerelease != "" || series.Build != "")
            {
                throw new ArgumentException("Not a minor version", nameof(series));
            }
            Guard.NotNull(commit, nameof(commit));
            Guard.NotNull(name, nameof(name));

            Series = series;
            Commit = commit;
            Name = name;
        }


        public SemVersion Series { get; }
        public CommitInfo Commit { get; }
        GitRefNameComponent Name { get; }

    }
}
