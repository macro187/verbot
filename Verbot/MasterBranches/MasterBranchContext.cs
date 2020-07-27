using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using MacroSemver;
using Verbot.Calculations;
using Verbot.Commits;
using Verbot.Refs;
using Verbot.Releases;

namespace Verbot.MasterBranches
{
    class MasterBranchContext
    {

        readonly RefContext RefContext;
        readonly ReleaseContext ReleaseContext;
        readonly CalculationContext CalculationContext;


        public MasterBranchContext(
            ReleaseContext releaseContext,
            RefContext refContext,
            CalculationContext calculationContext)
        {
            ReleaseContext = releaseContext;
            RefContext = refContext;
            CalculationContext = calculationContext;
        }


        IEnumerable<MasterBranchSpec> LatestMasterBranchPointsCache;
        IReadOnlyDictionary<GitRefNameComponent, MasterBranchSpec> LatestMasterBranchPointsByNameCache;


        public IEnumerable<MasterBranchSpec> LatestMasterBranchPoints =>
            LatestMasterBranchPointsCache ??=
                FindLatestMasterBranchPoints();


        public IReadOnlyDictionary<GitRefNameComponent, MasterBranchSpec> LatestMasterBranchPointsByName =>
            LatestMasterBranchPointsByNameCache ??=
                LatestMasterBranchPoints.ToDictionary(b => b.Name, b => b);


        IEnumerable<MasterBranchSpec> FindLatestMasterBranchPoints()
        {
            var leaves =
                Enumerable.Empty<CommitInfo>()
                    .Concat(ReleaseContext.ReleasesDescending.Select(r => r.Commit))
                    .Concat(RefContext.MasterBranches.Select(b => b.Target))
                    .ToList();

            foreach (var leaf in leaves)
            {
                CalculationContext.CalculateTo(leaf);
            }

            var candidates = new HashSet<CommitInfo>(leaves.SelectMany(leaf => leaf.GetCommitsSince(null)));
            var states = candidates.Select(c => CalculationContext.Calculate(c)).ToList();

            var latestCommitsInEachSeries =
                states
                    .GroupBy(commit => commit.ReleaseSeries)
                    .Select(commits =>
                        commits
                            .OrderBy(commit => commit.Version)
                            .Last())
                    .OrderByDescending(commit => commit.ReleaseSeries)
                    .ToList();

            var latestSeriesLatestCommit =
                latestCommitsInEachSeries.FirstOrDefault();

            var otherLatestCommits =
                latestCommitsInEachSeries.Skip(1);

            if (latestSeriesLatestCommit != null)
            {
                yield return
                    new MasterBranchSpec(
                        latestSeriesLatestCommit.ReleaseSeries,
                        latestSeriesLatestCommit.Commit,
                        new GitRefNameComponent("master"));
            }

            foreach (var commit in otherLatestCommits)
            {
                yield return
                    new MasterBranchSpec(
                        commit.ReleaseSeries,
                        commit.Commit,
                        new GitRefNameComponent($"{commit.Major}.{commit.Minor}-master"));
            }
        }


        public SemVersion CalculateMasterBranchSeries(BranchInfo @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));

            if (@ref.Name == "master")
            {
                return CalculationContext.Calculate(@ref.Target).ReleaseSeries;
            }

            var match = Regex.Match(@ref.Name, @"^(\d+)\.(\d+)-master$");
            if (match.Success)
            {
                return new SemVersion(
                    int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
            }

            return null;
        }


        GitRefNameComponent CalculateMasterBranchName(SemVersion version)
        {
            var series = version.Change(patch: 0, prerelease: "", build: "");
            var latestSeries = ReleaseContext.LatestRelease?.Version?.Change(patch: 0);
            var isLatest = latestSeries == null || series >= latestSeries;
            return
                isLatest
                    ? new GitRefNameComponent("master")
                    : new GitRefNameComponent($"{series.Major}.{series.Minor}-master");
        }

    }
}
