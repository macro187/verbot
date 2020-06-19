using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    partial class VerbotRepository
    {

        IEnumerable<MasterBranchInfo> MasterBranchesCache;
        IEnumerable<MasterBranchSpec> MasterBranchPointsCache;


        public IEnumerable<MasterBranchInfo> MasterBranches =>
            MasterBranchesCache ?? (MasterBranchesCache =
                Branches
                    .Select(b => (Ref: b, Series: CalculateMasterBranchSeries(b)))
                    .Where(b => b.Series != null)
                    .Select(b => new MasterBranchInfo(b.Ref, b.Series))
                    .OrderByDescending(b => b.Series)
                    .ToList());


        SemVersion CalculateMasterBranchSeries(RefInfo @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsBranch) throw new ArgumentException("Not a branch", nameof(@ref));

            if (@ref.Name == "master")
            {
                return GetCommitState(@ref.Target).ReleaseSeries;
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


        IEnumerable<MasterBranchSpec> MasterBranchPoints =>
            MasterBranchPointsCache ?? (MasterBranchPointsCache = FindMasterBranchPoints());


        IEnumerable<MasterBranchSpec> FindMasterBranchPoints()
        {
            var leaves =
                ReleasesDescending
                    .Select(r => r.Commit)
                .Concat(MasterBranches
                    .Select(b => b.Target))
                .ToList();

            foreach (var leaf in leaves)
            {
                Analyze(leaf);
            }

            var candidates = new HashSet<CommitInfo>(leaves.SelectMany(leaf => leaf.CommitsSince(null)));
            var states = candidates.Select(c => GetCommitState(c)).ToList();

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

    }
}
