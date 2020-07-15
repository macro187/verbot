using System.Linq;
using MacroExceptions;
using System.Diagnostics;
using MacroGit;
using MacroSemver;
using System.Collections.Generic;

namespace Verbot
{
    partial class VerbotRepository
    {

        public void CheckLocal()
        {
            // Issues:
            //
            // Error, later checks (and other operations) rely on that assumption
            //
            // Warn, later checks (and maybe other operations?) don't depend on it
            //
            // Any of the above => non-zero exit code
            //
            // Relationship to repair command: What can be fixed? Same as warnings?
            // 

            // Error
            CheckNoMergeCommits();
            CheckNoReleaseZero();
            CheckNoCommitsWithMultipleReleases();
            CheckNoMissingReleases();
            CheckReleaseOrdering();
            CheckMajorReleaseOrdering();
            CheckMinorReleaseOrdering();
            CheckPatchReleaseOrdering();

            // Warn
            CheckMajorReleaseSemverChanges();
            CheckMinorReleaseSemverChanges();
            CheckPatchReleaseSemverChanges();
            CheckNoMissingLatestBranches();
            CheckLatestBranchesAtCorrectReleases();
            CheckNoMissingMasterBranches();

            // Error
            CheckMasterBranchesInCorrectReleaseSeries();

            // Warn
            CheckMasterBranchesNotBehind();
        }


        public void CheckRemote()
        {
            CheckForRemoteBranchesAtUnknownCommits();
            CheckForRemoteBranchesNotBehindLocal();
            CheckForIncorrectRemoteTags();
        }


        void CheckNoUncommittedChanges()
        {
            if (GitRepository.HasUncommittedChanges())
                throw new UserException("Uncommitted changes in repository");
        }


        void CheckNoMergeCommits()
        {
            bool passed = true;

            var leaves =
                ReleasesDescending.Select(r => r.Commit)
                .Concat(MasterBranches.Select(b => b.Target));

            foreach (var leaf in leaves)
            {
                foreach (var commit in leaf.GetCommitsBackToBeginning())
                {
                    if (commit.ParentSha1s.Count > 1)
                    {
                        Trace.TraceError($"Merge commit {commit.Sha1}");
                        break;
                    }
                }
            }

            if (!passed)
            {
                throw new UserException($"Verbot does not support merge commits in release history");
            }
        }


        void CheckNoReleaseZero()
        {
            if (FindRelease(new SemVersion(0, 0, 0)) != null)
            {
                throw new UserException("Found release 0.0.0");
            }
        }


        void CheckNoCommitsWithMultipleReleases()
        {
            var passed = true;

            foreach (var releases in CommitReleaseLookup)
            {
                if (releases.Count() > 1)
                {
                    var sha1 = releases.Key.Sha1;
                    var releaseNames = string.Join(", ", releases.Select(t => t.Version));
                    Trace.TraceError($"Multiple releases on commit {sha1}: {releaseNames}");
                    passed = false;
                }
            }

            if (!passed)
            {
                throw new UserException("Commit(s) with multiple releases");
            }
        }


        void CheckNoMissingReleases()
        {
            var missingVersions =
                Enumerable.Empty<SemVersion>()
                    .Concat(FindMissingMajorReleases())
                    .Concat(FindMissingMinorReleases())
                    .Concat(FindMissingPatchReleases());

            if (missingVersions.Any())
            {
                foreach (var version in missingVersions.OrderBy(v => v))
                {
                    Trace.TraceError($"Missing release {version}");
                }

                throw new UserException("Missing release(s)");
            }
        }


        IEnumerable<SemVersion> FindMissingMajorReleases()
        {
            var latestRelease = ReleasesDescending.FirstOrDefault();
            if (latestRelease == null) yield break;
            for (var major = 1; major <= latestRelease.Version.Major; major++)
            {
                var version = new SemVersion(major, 0, 0);
                if (FindRelease(version) == null)
                {
                    yield return version;
                }
            }
        }


        IEnumerable<SemVersion> FindMissingMinorReleases()
        {
            foreach (var latestRelease in LatestMajorSeriesReleases)
            {
                for (var minor = 1; minor <= latestRelease.Version.Minor; minor++)
                {
                    var minorVersion = latestRelease.Version.Change(minor: minor, patch: 0);
                    if (FindRelease(minorVersion) == null)
                    {
                        yield return minorVersion;
                    }
                }
            }
        }


        IEnumerable<SemVersion> FindMissingPatchReleases()
        {
            foreach (var latestRelease in LatestMinorSeriesReleases)
            {
                for (var patch = 1; patch < latestRelease.Version.Patch; patch++)
                {
                    var patchVersion = latestRelease.Version.Change(patch: patch);
                    if (FindRelease(patchVersion) == null)
                    {
                        yield return patchVersion;
                    }
                }
            }
        }


        void CheckReleaseOrdering()
        {
            var passed = true;

            foreach (var release in ReleasesAscending)
            {
                var previousRelease = release.PreviousAncestralRelease;
                if (previousRelease == null) continue;
                if (release.Version < previousRelease.Version)
                {
                    Trace.TraceError($"Release {release.Version} descends from higher {previousRelease.Version}");
                    passed = false;
                }
            }

            if (!passed)
            {
                throw new UserException("Incorrect release ordering");
            }
        }


        void CheckMajorReleaseOrdering()
        {
            if (!MajorReleases.All(r => CheckMajorReleaseOrdering(r)))
            {
                throw new UserException("Invalid major release lineage");
            }
        }


        void CheckMinorReleaseOrdering()
        {
            if (!MinorReleases.All(r => CheckMinorReleaseOrdering(r)))
            {
                throw new UserException("Invalid minor release lineage");
            }
        }


        void CheckPatchReleaseOrdering()
        {
            if (!PatchReleases.All(r => CheckPatchReleaseOrdering(r)))
            {
                throw new UserException("Invalid patch release lineage");
            }
        }


        bool CheckMajorReleaseOrdering(ReleaseInfo release)
        {
            var version = release.Version;
            if (release.PreviousMajorRelease != null)
            {
                var previousMajorVersion = release.PreviousMajorRelease.Version;
                if (!release.Commit.IsDescendentOf(release.PreviousMajorRelease.Commit))
                {
                    Trace.TraceError($"Release {version} does not descend from {previousMajorVersion}");
                    return false;
                }
            }
            return true;
        }


        bool CheckMinorReleaseOrdering(ReleaseInfo release)
        {
            var version = release.Version;
            if (!release.Commit.IsDescendentOf(release.PreviousNumericMajorOrMinorRelease.Commit))
            {
                var previousMajorOrMinorVersion = release.PreviousNumericMajorOrMinorRelease.Version;
                Trace.TraceError($"Release {version} does not descend from {previousMajorOrMinorVersion}");
                return false;
            }
            return true;
        }


        bool CheckPatchReleaseOrdering(ReleaseInfo release)
        {
            var version = release.Version;
            if (!release.Commit.IsDescendentOf(release.PreviousNumericRelease.Commit))
            {
                var previousVersion = release.PreviousNumericRelease.Version;
                Trace.TraceError($"Release {version} does not descend from {previousVersion}");
                return false;
            }
            return true;
        }


        void CheckMajorReleaseSemverChanges()
        {
            foreach (var release in MajorReleases)
            {
                CheckMajorReleaseSemverChanges(release);
            }
        }


        void CheckMinorReleaseSemverChanges()
        {
            foreach (var release in MinorReleases)
            {
                CheckMinorReleaseSemverChanges(release);
            }
        }


        void CheckPatchReleaseSemverChanges()
        {
            foreach (var release in PatchReleases)
            {
                CheckPatchReleaseSemverChanges(release);
            }
        }


        void CheckMajorReleaseSemverChanges(ReleaseInfo release)
        {
            var version = release.Version;
            var breakingChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsBreaking);
            if (breakingChange == null)
            {
                var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";
                Trace.TraceWarning($"No breaking change(s) between {previousVersion} and {version}");
            }
        }


        void CheckMinorReleaseSemverChanges(ReleaseInfo release)
        {
            var version = release.Version;

            var breakingChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsBreaking);
            if (breakingChange != null)
            {
                var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";
                Trace.TraceWarning($"Breaking change between {previousVersion} and {version}");
                Trace.TraceWarning(breakingChange.Sha1);
                Trace.TraceWarning(breakingChange.Message);
            }

            var featureChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsFeature);
            if (featureChange == null)
            {
                var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";
                Trace.TraceWarning($"No feature change(s) between {previousVersion} and {version}");
            }
        }


        void CheckPatchReleaseSemverChanges(ReleaseInfo release)
        {
            var version = release.Version;

            var breakingChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsBreaking);
            if (breakingChange != null)
            {
                var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";
                Trace.TraceWarning($"Breaking change between {previousVersion} and {version}");
                Trace.TraceWarning(breakingChange.Sha1);
                Trace.TraceWarning(breakingChange.Message);
            }

            var featureChange = release.CommitsSincePreviousAncestralRelease.FirstOrDefault(c => c.IsFeature);
            if (featureChange != null)
            {
                var previousVersion = release.PreviousAncestralRelease?.Version ?? "beginning";
                Trace.TraceWarning($"Feature change(s) between {previousVersion} and {version}");
                Trace.TraceWarning(featureChange.Sha1);
                Trace.TraceWarning(featureChange.Message);
            }
        }


        void CheckNoMissingLatestBranches()
        {
            foreach (var branchThatShouldExist in GetLatestBranchesThatShouldExist())
            {
                var name = branchThatShouldExist.Name;
                var branch = FindBranch(name);
                if (branch == null)
                {
                    Trace.TraceWarning($"Missing {name} branch");
                    continue;
                }
            }
        }


        void CheckLatestBranchesAtCorrectReleases()
        {
            foreach (var branchThatShouldExist in GetLatestBranchesThatShouldExist())
            {
                var name = branchThatShouldExist.Name;
                var branch = FindBranch(name);
                if (branch == null) continue;

                var correctCommit = branchThatShouldExist.Release.Commit;
                if (branch.Target != correctCommit)
                {
                    Trace.TraceWarning($"{name} branch should be at commit {correctCommit.Sha1}");
                }
            }
        }


        void CheckNoMissingMasterBranches()
        {
            foreach (var spec in LatestMasterBranchPoints.OrderBy(spec => spec.Series))
            {
                if (!MasterBranches.Any(b => b.Name == spec.Name))
                {
                    Trace.TraceWarning($"Missing {spec.Name} branch");
                }
            }
        }


        void CheckMasterBranchesInCorrectReleaseSeries()
        {
            var passed = true;

            foreach (var branch in MasterBranches)
            {
                var state = Calculate(branch.Target);
                if (branch.Series != state.ReleaseSeries)
                {
                    Trace.TraceError($"{branch.Name} on incorrect release series {state.ReleaseSeries}");
                    passed = false;
                }
            }

            if (!passed)
            {
                throw new UserException("Master branch(es) on incorrect release series");
            }
        }


        void CheckMasterBranchesNotBehind()
        {
            foreach (var branch in MasterBranches)
            {
                var latestKnownPoint = LatestMasterBranchPointsByName[branch.Name];
                if (!branch.Target.IsDescendentOf(latestKnownPoint.Commit))
                {
                    var name = branch.Name;
                    var series = $"{branch.Series.Major}.{branch.Series.Minor}";
                    var latestSha1 = latestKnownPoint.Commit.Sha1;
                    Trace.TraceWarning($"Branch {name} behind latest commit in {series} series {latestSha1}");
                }
            }
        }

    }
}
