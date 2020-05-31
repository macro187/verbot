using MacroGit;
using MacroGuards;

namespace Verbot
{
    class LatestBranchSpec
    {

        public LatestBranchSpec(GitRefNameComponent name, ReleaseInfo release)
        {
            Guard.NotNull(name, nameof(name));
            Guard.NotNull(release, nameof(release));
            Name = name;
            Release = release;
        }


        public GitRefNameComponent Name { get; }
        public ReleaseInfo Release { get; }

    }
}
