using System.Linq;
using MacroGit;
using MacroSemver;
using Verbot.LatestBranches;
using Verbot.MasterBranches;
using Verbot.Refs;
using Verbot.Releases;
using static Verbot.Checks.CheckFailure;

namespace Verbot.Checks
{
    class CheckContext
    {

        readonly GitRepository GitRepository;
        readonly MasterBranchContext MasterBranchContext;
        readonly LatestBranchContext LatestBranchContext;
        readonly ReleaseContext ReleaseContext;
        readonly RefContext RefContext;


        public CheckContext(
            GitRepository gitRepository,
            MasterBranchContext masterBranchContext,
            LatestBranchContext latestBranchContext,
            ReleaseContext releaseContext,
            RefContext refContext)
        {
            GitRepository = gitRepository;
            MasterBranchContext = masterBranchContext;
            LatestBranchContext = latestBranchContext;
            ReleaseContext = releaseContext;
            RefContext = refContext;
        }


        public CheckFailure Check() =>
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
            CheckMasterBranchesInCorrectPlaces() ??
            null;


        public CheckFailure CheckNoMergeCommits()
        {
            var leaves =
                ReleaseContext.ReleasesDescending.Select(r => r.Commit)
                .Concat(RefContext.MasterBranches.Select(b => b.Target));

            foreach (var leaf in leaves)
            {
                foreach (var commit in leaf.GetCommitsBackToBeginning())
                {
                    if (commit.ParentSha1s.Count > 1)
                    {
                        return Fail(
                            $"Merge commit {commit.Sha1}",
                            "Merge commits in the release history are not supported");
                    }
                }
            }

            return null;
        }


        public CheckFailure CheckNoReleaseZero()
        {
            var releaseZero = ReleaseContext.FindRelease(new SemVersion(0, 0, 0));
            if (releaseZero != null)
            {
                return Fail(
                    "Found invalid release 0.0.0",
                    $"Deleting {releaseZero.Tag.Name} release tag from {releaseZero.Commit.Sha1}",
                    () => {
                        GitRepository.DeleteTag(releaseZero.Tag.Name);
                    });
            }

            return null;
        }


        public CheckFailure CheckNoCommitsWithMultipleReleases()
        {
            foreach (var releases in ReleaseContext.CommitReleaseLookup)
            {
                if (releases.Count() > 1)
                {
                    var sha1 = releases.Key.Sha1;
                    var releaseNames = string.Join(", ", releases.Select(t => t.Version));
                    return Fail(
                        $"Multiple releases on commit {sha1}: {releaseNames}",
                        "Multiple releases on the same commit not supported (yet)");
                }
            }

            return null;
        }


        public CheckFailure CheckNoMissingMajorReleases()
        {
            var latestRelease = ReleaseContext.ReleasesDescending.FirstOrDefault();
            if (latestRelease == null) return null;

            for (var major = 1; major <= latestRelease.Version.Major; major++)
            {
                var version = new SemVersion(major, 0, 0);
                if (ReleaseContext.FindRelease(version) == null)
                {
                    return Fail(
                        $"Missing release {version}",
                        "Correct revision history manually or give up");
                }
            }

            return null;
        }


        public CheckFailure CheckNoMissingMinorReleases()
        {
            foreach (var latestRelease in ReleaseContext.LatestMajorSeriesReleases)
            {
                for (var minor = 1; minor <= latestRelease.Version.Minor; minor++)
                {
                    var minorVersion = latestRelease.Version.Change(minor: minor, patch: 0);
                    if (ReleaseContext.FindRelease(minorVersion) == null)
                    {
                        return Fail(
                            $"Missing release {minorVersion}",
                            "Correct revision history manually or give up");
                    }
                }
            }

            return null;
        }


        public CheckFailure CheckNoMissingPatchReleases()
        {
            foreach (var latestRelease in ReleaseContext.LatestMinorSeriesReleases)
            {
                for (var patch = 1; patch < latestRelease.Version.Patch; patch++)
                {
                    var patchVersion = latestRelease.Version.Change(patch: patch);
                    if (ReleaseContext.FindRelease(patchVersion) == null)
                    {
                        return Fail(
                            $"Missing release {patchVersion}",
                            "Correct revision history manually or give up");
                    }
                }
            }

            return null;
        }


        public CheckFailure CheckReleaseOrdering()
        {
            foreach (var release in ReleaseContext.ReleasesAscending)
            {
                var previousRelease = release.PreviousAncestralRelease;
                if (previousRelease == null) continue;
                if (release.Version < previousRelease.Version)
                {
                    return Fail(
                        $"Release {release.Version} descends from higher {previousRelease.Version}",
                        "Correct revision history manually or give up");
                }
            }

            return null;
        }


        public CheckFailure CheckMajorReleaseOrdering()
        {
            foreach (var release in ReleaseContext.MajorReleases)
            {
                if (release.PreviousMajorRelease != null)
                {
                    if (!release.Commit.IsDescendentOf(release.PreviousMajorRelease.Commit))
                    {
                        var version = release.Version;
                        var previousMajorVersion = release.PreviousMajorRelease.Version;
                        return Fail(
                            $"Release {version} does not descend from {previousMajorVersion}",
                            "Correct revision history manually or give up");
                    }
                }
            }

            return null;
        }


        public CheckFailure CheckMinorReleaseOrdering()
        {
            foreach (var release in ReleaseContext.MinorReleases)
            {
                if (!release.Commit.IsDescendentOf(release.PreviousNumericMajorOrMinorRelease.Commit))
                {
                    var version = release.Version;
                    var previousMajorOrMinorVersion = release.PreviousNumericMajorOrMinorRelease.Version;
                    return Fail(
                        $"Release {version} does not descend from {previousMajorOrMinorVersion}",
                        "Correct revision history manually or give up");
                }
            }

            return null;
        }


        public CheckFailure CheckPatchReleaseOrdering()
        {
            foreach (var release in ReleaseContext.PatchReleases)
            {
                if (!release.Commit.IsDescendentOf(release.PreviousNumericRelease.Commit))
                {
                    var version = release.Version;
                    var previousVersion = release.PreviousNumericRelease.Version;
                    return Fail(
                        $"Release {version} does not descend from {previousVersion}",
                        "Correct revision history manually or give up");
                }
            }

            return null;
        }


        public CheckFailure CheckMajorReleaseSemverChanges()
        {
            foreach (var release in ReleaseContext.MajorReleases)
            {
                var breakingChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsBreaking);
                if (breakingChange == null)
                {
                    var version = release.Version;
                    var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";
                    return Fail(
                        $"No breaking change(s) between {previousVersion} and {version}",
                        "Correct revision history manually or give up");
                }
            }

            return null;
        }


        public CheckFailure CheckMinorReleaseSemverChanges()
        {
            foreach (var release in ReleaseContext.MinorReleases)
            {
                var version = release.Version;
                var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";

                var breakingChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsBreaking);
                if (breakingChange != null)
                {
                    var sha1 = breakingChange.Sha1;
                    return Fail(
                        $"Breaking change {sha1} between {previousVersion} and {version}",
                        "Correct revision history manually or give up");
                }

                var featureChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsFeature);
                if (featureChange == null)
                {
                    return Fail(
                        $"No feature change(s) between {previousVersion} and {version}",
                        "Correct revision history manually or give up");
                }
            }

            return null;
        }


        public CheckFailure CheckPatchReleaseSemverChanges()
        {
            foreach (var release in ReleaseContext.PatchReleases)
            {
                var version = release.Version;
                var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";

                var breakingChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsBreaking);
                if (breakingChange != null)
                {
                    var sha1 = breakingChange.Sha1;
                    return Fail(
                        $"Breaking change {sha1} between {previousVersion} and {version}",
                        "Correct revision history manually or give up");
                }

                var featureChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsFeature);
                if (featureChange != null)
                {
                    var sha1 = featureChange.Sha1;
                    return Fail(
                        $"Feature change {sha1} between {previousVersion} and {version}",
                        "Correct revision history manually or give up");
                }
            }

            return null;
        }


        public CheckFailure CheckNoMissingLatestBranches()
        {
            foreach (var branchThatShouldExist in LatestBranchContext.GetLatestBranchesThatShouldExist())
            {
                var name = branchThatShouldExist.Name;
                var branch = RefContext.FindBranch(name);
                if (branch == null)
                {
                    var version = branchThatShouldExist.Release.Version;
                    return Fail(
                        $"Missing {name} branch",
                        $"Create {name} branch at {version}");
                }
            }

            return null;
        }


        public CheckFailure CheckLatestBranchesAtCorrectReleases()
        {
            foreach (var branchThatShouldExist in LatestBranchContext.GetLatestBranchesThatShouldExist())
            {
                var name = branchThatShouldExist.Name;
                var branch = RefContext.FindBranch(name);
                if (branch == null) continue;

                var correctCommit = branchThatShouldExist.Release.Commit;
                if (branch.Target != correctCommit)
                {
                    var version = branchThatShouldExist.Release.Version;
                    return Fail(
                        $"{name} branch should be at {version}",
                        $"Move {name} branch to {version}");
                }
            }

            return null;
        }


        public CheckFailure CheckNoMissingMasterBranches()
        {
            foreach (var spec in MasterBranchContext.LatestMasterBranchPoints.OrderBy(spec => spec.Series))
            {
                if (!RefContext.MasterBranches.Any(b => b.Name == spec.Name))
                {
                    return Fail(
                        $"Missing {spec.Name} branch",
                        $"Create {spec.Name} branch at {spec.Commit.Sha1}");
                }
            }

            return null;
        }


        public CheckFailure CheckMasterBranchesInCorrectPlaces()
        {
            foreach (var spec in MasterBranchContext.LatestMasterBranchPoints.OrderBy(spec => spec.Series))
            {
                var branch = RefContext.MasterBranches.Single(b => b.Name == spec.Name);
                if (branch.Target != spec.Commit)
                {
                    return Fail(
                        $"{branch.Name} at incorrect commit {branch.Target.Sha1}",
                        $"Move {branch.Name} to {spec.Commit.Sha1} without losing its current commits");
                }
            }

            return null;
        }

    }
}
