using System.Linq;
using MacroSemver;
using Verbot.Calculations;
using Verbot.LatestBranches;
using Verbot.MasterBranches;
using Verbot.Refs;
using Verbot.Releases;
using static Verbot.Checks.CheckFailure;

namespace Verbot.Checks
{
    class CheckContext
    {

        readonly MasterBranchContext MasterBranchContext;
        readonly LatestBranchContext LatestBranchContext;
        readonly ReleaseContext ReleaseContext;
        readonly RefContext RefContext;
        readonly CalculationContext CalculationContext;


        public CheckContext(
            MasterBranchContext masterBranchContext,
            LatestBranchContext latestBranchContext,
            ReleaseContext releaseContext,
            RefContext refContext,
            CalculationContext calculationContext)
        {
            MasterBranchContext = masterBranchContext;
            LatestBranchContext = latestBranchContext;
            ReleaseContext = releaseContext;
            RefContext = refContext;
            CalculationContext = calculationContext;
        }


        public CheckFailure CheckLocal() =>
            CheckNoMergeCommits() ??
            CheckNoReleaseZero() ??
            CheckNoCommitsWithMultipleReleases() ??
            CheckNoMissingMajorReleases() ??
            CheckNoMissingMinorReleases() ??
            CheckNoMissingPatchReleases() ??
            CheckReleaseOrdering() ??
            CheckMajorReleaseOrdering() ??
            CheckMinorReleaseOrdering() ??
            CheckPatchReleaseOrdering() ??
            CheckMajorReleaseSemverChanges() ??
            CheckMinorReleaseSemverChanges() ??
            CheckPatchReleaseSemverChanges() ??
            CheckNoMissingLatestBranches() ??
            CheckLatestBranchesAtCorrectReleases() ??
            CheckNoMissingMasterBranches() ??
            CheckMasterBranchesInCorrectReleaseSeries() ??
            CheckMasterBranchesNotBehind() ??
            null;


        CheckFailure CheckNoMergeCommits()
        {
            var leaves =
                ReleaseContext.ReleasesDescending.Select(r => r.Commit)
                .Concat(MasterBranchContext.MasterBranches.Select(b => b.Target));

            foreach (var leaf in leaves)
            {
                foreach (var commit in leaf.GetCommitsBackToBeginning())
                {
                    if (commit.ParentSha1s.Count > 1)
                    {
                        return Fail($"Merge commit {commit.Sha1}");
                    }
                }
            }

            return null;
        }


        CheckFailure CheckNoReleaseZero()
        {
            if (ReleaseContext.FindRelease(new SemVersion(0, 0, 0)) != null)
            {
                return Fail("Found release 0.0.0");
            }

            return null;
        }


        CheckFailure CheckNoCommitsWithMultipleReleases()
        {
            foreach (var releases in ReleaseContext.CommitReleaseLookup)
            {
                if (releases.Count() > 1)
                {
                    var sha1 = releases.Key.Sha1;
                    var releaseNames = string.Join(", ", releases.Select(t => t.Version));
                    return Fail($"Multiple releases on commit {sha1}: {releaseNames}");
                }
            }

            return null;
        }


        CheckFailure CheckNoMissingMajorReleases()
        {
            var latestRelease = ReleaseContext.ReleasesDescending.FirstOrDefault();
            if (latestRelease == null) return null;

            for (var major = 1; major <= latestRelease.Version.Major; major++)
            {
                var version = new SemVersion(major, 0, 0);
                if (ReleaseContext.FindRelease(version) == null)
                {
                    return Fail($"Missing release {version}");
                }
            }

            return null;
        }


        CheckFailure CheckNoMissingMinorReleases()
        {
            foreach (var latestRelease in ReleaseContext.LatestMajorSeriesReleases)
            {
                for (var minor = 1; minor <= latestRelease.Version.Minor; minor++)
                {
                    var minorVersion = latestRelease.Version.Change(minor: minor, patch: 0);
                    if (ReleaseContext.FindRelease(minorVersion) == null)
                    {
                        return Fail($"Missing release {minorVersion}");
                    }
                }
            }

            return null;
        }


        CheckFailure CheckNoMissingPatchReleases()
        {
            foreach (var latestRelease in ReleaseContext.LatestMinorSeriesReleases)
            {
                for (var patch = 1; patch < latestRelease.Version.Patch; patch++)
                {
                    var patchVersion = latestRelease.Version.Change(patch: patch);
                    if (ReleaseContext.FindRelease(patchVersion) == null)
                    {
                        return Fail($"Missing release {patchVersion}");
                    }
                }
            }

            return null;
        }


        CheckFailure CheckReleaseOrdering()
        {
            foreach (var release in ReleaseContext.ReleasesAscending)
            {
                var previousRelease = release.PreviousAncestralRelease;
                if (previousRelease == null) continue;
                if (release.Version < previousRelease.Version)
                {
                    return Fail($"Release {release.Version} descends from higher {previousRelease.Version}");
                }
            }

            return null;
        }


        CheckFailure CheckMajorReleaseOrdering()
        {
            foreach (var release in ReleaseContext.MajorReleases)
            {
                if (release.PreviousMajorRelease != null)
                {
                    if (!release.Commit.IsDescendentOf(release.PreviousMajorRelease.Commit))
                    {
                        var version = release.Version;
                        var previousMajorVersion = release.PreviousMajorRelease.Version;
                        return Fail($"Release {version} does not descend from {previousMajorVersion}");
                    }
                }
            }

            return null;
        }


        CheckFailure CheckMinorReleaseOrdering()
        {
            foreach (var release in ReleaseContext.MinorReleases)
            {
                if (!release.Commit.IsDescendentOf(release.PreviousNumericMajorOrMinorRelease.Commit))
                {
                    var version = release.Version;
                    var previousMajorOrMinorVersion = release.PreviousNumericMajorOrMinorRelease.Version;
                    return Fail($"Release {version} does not descend from {previousMajorOrMinorVersion}");
                }
            }

            return null;
        }


        CheckFailure CheckPatchReleaseOrdering()
        {
            foreach (var release in ReleaseContext.PatchReleases)
            {
                if (!release.Commit.IsDescendentOf(release.PreviousNumericRelease.Commit))
                {
                    var version = release.Version;
                    var previousVersion = release.PreviousNumericRelease.Version;
                    return Fail($"Release {version} does not descend from {previousVersion}");
                }
            }

            return null;
        }


        CheckFailure CheckMajorReleaseSemverChanges()
        {
            foreach (var release in ReleaseContext.MajorReleases)
            {
                var breakingChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsBreaking);
                if (breakingChange == null)
                {
                    var version = release.Version;
                    var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";
                    return Fail($"No breaking change(s) between {previousVersion} and {version}");
                }
            }

            return null;
        }


        CheckFailure CheckMinorReleaseSemverChanges()
        {
            foreach (var release in ReleaseContext.MinorReleases)
            {
                var version = release.Version;
                var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";

                var breakingChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsBreaking);
                if (breakingChange != null)
                {
                    var sha1 = breakingChange.Sha1;
                    return Fail($"Breaking change {sha1} between {previousVersion} and {version}");
                }

                var featureChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsFeature);
                if (featureChange == null)
                {
                    return Fail($"No feature change(s) between {previousVersion} and {version}");
                }
            }

            return null;
        }


        CheckFailure CheckPatchReleaseSemverChanges()
        {
            foreach (var release in ReleaseContext.PatchReleases)
            {
                var version = release.Version;
                var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";

                var breakingChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsBreaking);
                if (breakingChange != null)
                {
                    var sha1 = breakingChange.Sha1;
                    return Fail($"Breaking change {sha1} between {previousVersion} and {version}");
                }

                var featureChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsFeature);
                if (featureChange != null)
                {
                    var sha1 = featureChange.Sha1;
                    return Fail($"Feature change {sha1} between {previousVersion} and {version}");
                }
            }

            return null;
        }


        CheckFailure CheckNoMissingLatestBranches()
        {
            foreach (var branchThatShouldExist in LatestBranchContext.GetLatestBranchesThatShouldExist())
            {
                var name = branchThatShouldExist.Name;
                var branch = RefContext.FindBranch(name);
                if (branch == null)
                {
                    return Fail($"Missing {name} branch");
                }
            }

            return null;
        }


        CheckFailure CheckLatestBranchesAtCorrectReleases()
        {
            foreach (var branchThatShouldExist in LatestBranchContext.GetLatestBranchesThatShouldExist())
            {
                var name = branchThatShouldExist.Name;
                var branch = RefContext.FindBranch(name);
                if (branch == null) continue;

                var correctCommit = branchThatShouldExist.Release.Commit;
                if (branch.Target != correctCommit)
                {
                    return Fail($"{name} branch should be at commit {correctCommit.Sha1}");
                }
            }

            return null;
        }


        CheckFailure CheckNoMissingMasterBranches()
        {
            foreach (var spec in MasterBranchContext.LatestMasterBranchPoints.OrderBy(spec => spec.Series))
            {
                if (!MasterBranchContext.MasterBranches.Any(b => b.Name == spec.Name))
                {
                    return Fail($"Missing {spec.Name} branch");
                }
            }

            return null;
        }


        CheckFailure CheckMasterBranchesInCorrectReleaseSeries()
        {
            foreach (var branch in MasterBranchContext.MasterBranches)
            {
                var state = CalculationContext.Calculate(branch.Target);
                if (branch.Series != state.ReleaseSeries)
                {
                    return Fail($"{branch.Name} on incorrect release series {state.ReleaseSeries}");
                }
            }

            return null;
        }


        CheckFailure CheckMasterBranchesNotBehind()
        {
            foreach (var branch in MasterBranchContext.MasterBranches)
            {
                var latestKnownPoint = MasterBranchContext.LatestMasterBranchPointsByName[branch.Name];
                if (!branch.Target.IsDescendentOf(latestKnownPoint.Commit))
                {
                    var name = branch.Name;
                    var series = $"{branch.Series.Major}.{branch.Series.Minor}";
                    var latestSha1 = latestKnownPoint.Commit.Sha1;
                    return Fail($"Branch {name} behind latest commit in {series} series {latestSha1}");
                }
            }

            return null;
        }

    }
}
