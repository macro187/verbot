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
            CheckForVersionLocations();
            CheckForConflictingVersions();
            CheckForMissingVersions();
            CheckForReleaseZero();
            CheckForMultipleReleasesFromSingleCommit();
            CheckForMissingReleases();
            CheckReleaseLineage();
            CheckReleaseSemverCommits();
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


        void CheckForReleaseZero()
        {
            if (FindRelease(new SemVersion(0, 0, 0)) != null)
            {
                throw new UserException("Found release 0.0.0");
            }
        }


        void CheckForMultipleReleasesFromSingleCommit()
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


        void CheckForMissingReleases()
        {
            var missingVersions =
                Enumerable.Empty<SemVersion>()
                    .Concat(CheckForMissingMajorReleases())
                    .Concat(CheckForMissingMinorReleases())
                    .Concat(CheckForMissingPatchReleases());

            if (missingVersions.Any())
            {
                foreach (var version in missingVersions.OrderBy(v => v))
                {
                    Trace.TraceError($"Missing release {version}");
                }

                throw new UserException("Missing release(s)");
            }
        }


        IEnumerable<SemVersion> CheckForMissingMajorReleases()
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


        IEnumerable<SemVersion> CheckForMissingMinorReleases()
        {
            var latestMajorSeriesReleases =
                ReleasesAscending
                    .GroupBy(r => r.Version.Change(minor: 0, patch: 0))
                    .Select(g => g.OrderBy(r => r.Version).Last())
                    .OrderBy(r => r.Version);

            foreach (var latestRelease in latestMajorSeriesReleases)
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


        IEnumerable<SemVersion> CheckForMissingPatchReleases()
        {
            var latestMinorSeriesReleases =
                ReleasesAscending
                    .GroupBy(r => r.Version.Change(patch: 0))
                    .Select(g => g.OrderBy(r => r.Version).Last())
                    .OrderBy(r => r.Version);

            foreach (var latestRelease in latestMinorSeriesReleases)
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


        void CheckReleaseLineage()
        {
            var passed =
                ReleasesAscending.Aggregate(true, (result, release) =>
                    result &=
                        release.IsMajor
                            ? CheckMajorReleaseLineage(release)
                        : release.IsMinor
                            ? CheckMinorReleaseLineage(release)
                        : CheckPatchReleaseLineage(release));

            if (!passed)
            {
                throw new UserException("Invalid release lineage");
            }
        }


        bool CheckMajorReleaseLineage(ReleaseInfo release)
        {
            var version = release.Version;
            if (release.PreviousMajor != null)
            {
                var previousMajorVersion = release.PreviousMajor.Version;
                if (!release.Commit.DescendsFrom(release.PreviousMajor.Commit))
                {
                    Trace.TraceError($"Release {version} does not descend from {previousMajorVersion}");
                    return false;
                }
            }
            return true;
        }


        bool CheckMinorReleaseLineage(ReleaseInfo release)
        {
            var version = release.Version;
            if (!release.Commit.DescendsFrom(release.PreviousMinorNumeric.Commit))
            {
                var previousMinorVersion = release.PreviousMinorNumeric.Version;
                Trace.TraceError($"Release {version} does not descend from {previousMinorVersion}");
                return false;
            }
            return true;
        }


        bool CheckPatchReleaseLineage(ReleaseInfo release)
        {
            var version = release.Version;
            if (!release.Commit.DescendsFrom(release.PreviousNumeric.Commit))
            {
                var previousVersion = release.PreviousNumeric.Version;
                Trace.TraceError($"Release {version} does not descend from {previousVersion}");
                return false;
            }
            return true;
        }


        void CheckReleaseSemverCommits()
        {
            foreach (var release in ReleasesAscending)
            {
                if (release.IsMajor)
                {
                    CheckMajorReleaseSemverCommits(release);
                }
                else if (release.IsMinor)
                {
                    CheckMinorReleaseSemverCommits(release);
                }
                else
                {
                    CheckPatchReleaseSemverCommits(release);
                }
            }
        }


        void CheckMajorReleaseSemverCommits(ReleaseInfo release)
        {
            var version = release.Version;
            var breakingChange = release.CommitsSincePreviousAncestor.FirstOrDefault(c => c.IsBreaking);
            if (breakingChange == null)
            {
                var previousVersion = release.PreviousAncestor?.Version ?? "beginning";
                Trace.TraceWarning($"No breaking change(s) between {previousVersion} and {version}");
            }
        }


        void CheckMinorReleaseSemverCommits(ReleaseInfo release)
        {
            var version = release.Version;

            var breakingChange = release.CommitsSincePreviousAncestor.FirstOrDefault(c => c.IsBreaking);
            if (breakingChange != null)
            {
                var previousVersion = release.PreviousAncestor?.Version ?? "beginning";
                Trace.TraceWarning($"Breaking change between {previousVersion} and {version}");
                Trace.TraceWarning(breakingChange.Sha1);
                Trace.TraceWarning(breakingChange.Message);
            }

            var featureChange = release.CommitsSincePreviousAncestor.FirstOrDefault(c => c.IsFeature);
            if (featureChange == null)
            {
                var previousVersion = release.PreviousAncestor?.Version ?? "beginning";
                Trace.TraceWarning($"No feature change(s) between {previousVersion} and {version}");
            }
        }


        void CheckPatchReleaseSemverCommits(ReleaseInfo release)
        {
            var version = release.Version;

            var breakingChange = release.CommitsSincePreviousAncestor.FirstOrDefault(c => c.IsBreaking);
            if (breakingChange != null)
            {
                var previousVersion = release.PreviousAncestor?.Version ?? "beginning";
                Trace.TraceWarning($"Breaking change between {previousVersion} and {version}");
                Trace.TraceWarning(breakingChange.Sha1);
                Trace.TraceWarning(breakingChange.Message);
            }

            var featureChange = release.CommitsSincePreviousAncestor.FirstOrDefault(c => c.IsFeature);
            if (featureChange != null)
            {
                var previousVersion = release.PreviousAncestor?.Version ?? "beginning";
                Trace.TraceWarning($"Feature change(s) between {previousVersion} and {version}");
                Trace.TraceWarning(featureChange.Sha1);
                Trace.TraceWarning(featureChange.Message);
            }
        }

    }
}
