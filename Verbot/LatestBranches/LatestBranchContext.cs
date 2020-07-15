using System.Collections.Generic;
using System.Linq;
using MacroGit;

namespace Verbot
{
    class LatestBranchContext
    {

        readonly ReleaseContext ReleaseContext;


        public LatestBranchContext(ReleaseContext releaseContext)
        {
            ReleaseContext = releaseContext;
        }


        public IEnumerable<LatestBranchSpec> GetLatestBranchesThatShouldExist()
        {
            var latest = ReleaseContext.ReleasesAscending.LastOrDefault();
            if (latest != null)
            {
                yield return new LatestBranchSpec(new GitRefNameComponent("latest"), latest);
            }

            foreach (var release in ReleaseContext.LatestMajorSeriesReleases)
            {
                var major = release.Version.Major;
                yield return new LatestBranchSpec(new GitRefNameComponent($"{major}-latest"), release);
            }

            foreach (var release in ReleaseContext.LatestMinorSeriesReleases)
            {
                var major = release.Version.Major;
                var minor = release.Version.Minor;
                yield return new LatestBranchSpec(new GitRefNameComponent($"{major}.{minor}-latest"), release);
            }
        }

    }
}
