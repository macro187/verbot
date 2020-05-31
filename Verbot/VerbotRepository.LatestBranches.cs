using System.Collections.Generic;
using System.Linq;
using MacroGit;

namespace Verbot
{
    partial class VerbotRepository
    {

        IEnumerable<LatestBranchSpec> GetLatestBranchesThatShouldExist()
        {
            var latest = ReleasesAscending.LastOrDefault();
            if (latest != null)
            {
                yield return new LatestBranchSpec(new GitRefNameComponent("latest"), latest);
            }

            foreach (var release in LatestMajorSeriesReleases)
            {
                var major = release.Version.Major;
                yield return new LatestBranchSpec(new GitRefNameComponent($"{major}-latest"), release);
            }

            foreach (var release in LatestMinorSeriesReleases)
            {
                var major = release.Version.Major;
                var minor = release.Version.Minor;
                yield return new LatestBranchSpec(new GitRefNameComponent($"{major}.{minor}-latest"), release);
            }
        }

    }
}
