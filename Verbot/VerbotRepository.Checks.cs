using System.Linq;
using MacroExceptions;
using System.Diagnostics;
using MacroGit;
using System;
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
            CheckForMissingReleaseTags();
            CheckReleaseLineage();
            CheckReleaseSemverCommits();
        }


        public void CheckRemote()
        {
            CheckForRemoteBranchesAtUnknownCommits();
            CheckForRemoteBranchesNotBehindLocal();
            CheckForIncorrectRemoteTags();
        }


        void CheckForVersionLocations()
        {
            var locations = FindVersionLocations();

            if (locations.Count == 0)
            {
                throw new UserException("No locations found in repository to record version");
            }
        }


        void CheckForConflictingVersions()
        {
            var locations = FindVersionLocations();

            var distinctVersions =
                locations
                    .Select(location => location.GetVersion())
                    .Where(version => version != null)
                    .Distinct()
                    .ToList();

            if (distinctVersions.Count == 1)
            {
                return;
            }

            Trace.TraceError("Conflicting versions found in repository:");
            foreach (var location in locations)
            {
                var version = location.GetVersion() ?? "(none)";
                var description = location.Description;
                Trace.TraceError($"  {version} in {description}");
            }
            Trace.TraceError("Consider re-setting the correct current version using the 'set' command.");

            throw new UserException("Conflicting versions found in repository");
        }


        void CheckForMissingVersions()
        {
            var locations = FindVersionLocations();
            var missingLocations =
                locations
                    .Where(location => location.GetVersion() == null)
                    .ToList();

            if (missingLocations.Count == 0)
            {
                return;
            }

            Trace.TraceWarning($"Missing versions in some location(s) in the repository:");
            foreach (var location in missingLocations)
            {
                Trace.TraceWarning($"  {location.Description}");
            }
            Trace.TraceWarning("Consider re-setting the current version using the 'set' command.");
        }


        void CheckNoUncommittedChanges()
        {
            if (GitRepository.HasUncommittedChanges())
                throw new UserException("Uncommitted changes in repository");
        }


        void CheckVersionHasNotBeenReleased()
        {
            var releaseVersion = ReadFromVersionLocations().Change(null, null, null, "", "");
            if (GitRepository.GetTags().Any(t => t.Name == releaseVersion))
                throw new UserException("Current version has already been released");
        }


        void CheckVersionIsMasterPrerelease()
        {
            var version = ReadFromVersionLocations();
            if (version.Prerelease != "master")
                throw new UserException("Expected current version to be a -master prerelease");
        }


        void CheckVersionIsReleaseOrMasterPrerelease()
        {
            var version = ReadFromVersionLocations();
            if (!(version.Prerelease == "" || version.Prerelease == "master"))
                throw new UserException("Expected current version to be a release or -master prerelease");
        }


        void CheckMasterBranchIsTrackingHighestVersion()
        {
            if (MasterBranches.Any(mb => mb.Name == "master") && MasterBranches.First().Name != "master")
                throw new UserException("Expected master branch to be tracking the latest version");
        }


        void CheckOnCorrectMasterBranchForVersion()
        {
            var minorVersion = ReadFromVersionLocations().Change(null, null, 0, "", "");
            var expectedCurrentBranch =
                MasterBranches
                    .Where(mb => mb.Version == minorVersion)
                    .Select(mb => mb.Name)
                    .SingleOrDefault();
            if (expectedCurrentBranch == null)
                throw new UserException("No master branch found for current version");
            if (GitRepository.GetBranch() != expectedCurrentBranch)
                throw new UserException("Expected to be on branch " + expectedCurrentBranch);
        }


        void CheckNotSkippingRelease(bool major, bool minor)
        {
            var patch = !(major || minor);
            var version = ReadFromVersionLocations();
            if (version.Prerelease != "master") return;
            var patchString = FormattableString.Invariant($"{version.Major}.{version.Minor}.{version.Patch}");
            var minorString = FormattableString.Invariant($"{version.Major}.{version.Minor}");
            if (patch)
                throw new UserException(FormattableString.Invariant(
                    $"No need to increment patch when {patchString} hasn't been released yet"));
            if (minor && version.Patch == 0)
                throw new UserException(FormattableString.Invariant(
                    $"No need to increment minor when {patchString} hasn't been released yet"));
            if (major && version.Minor == 0 && version.Patch == 0)
                throw new UserException(FormattableString.Invariant(
                    $"No need to increment major when {minorString} hasn't been released yet"));
        }


        void CheckNotAdvancingToLatestVersionOnNonMasterBranch(SemVersion newVersion)
        {
            if (!MasterBranches.Any()) return;
            var newMinorVersion = newVersion.Change(null, null, 0, "", "");
            if (newMinorVersion > MasterBranches.First().Version && GitRepository.GetBranch() != "master")
                throw new UserException("Must be on master branch to advance to latest version");
        }


        void CheckForIncorrectRemoteTags()
        {
            var verbotTagsWithRemote = FindReleaseTagsWithRemote();

            var incorrectRemoteTags =
                verbotTagsWithRemote
                    .Where(t => t.RemoteTarget != null)
                    .Where(t => t.RemoteTarget != t.Target)
                    .ToList();

            if (!incorrectRemoteTags.Any()) return;

            foreach (var tag in incorrectRemoteTags)
            {
                Trace.TraceError($"Remote tag {tag.Name} at {tag.RemoteTarget} local {tag.Target}");
            }

            throw new UserException("Incorrect remote tag(s) found");
        }


        void CheckForRemoteBranchesAtUnknownCommits()
        {
            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();

            var remoteBranchesAtUnknownCommits =
                verbotBranchesWithRemote
                    .Where(b => b.RemoteTarget != null)
                    .Where(b => !GitRepository.Exists(b.RemoteTarget))
                    .ToList();

            if (!remoteBranchesAtUnknownCommits.Any()) return;

            foreach (var branch in remoteBranchesAtUnknownCommits)
            {
                Trace.TraceError($"Remote branch {branch.Name} at unknown commit {branch.RemoteTarget}");
            }

            throw new UserException("Remote branch(es) at unknown commits");
        }


        void CheckForRemoteBranchesNotBehindLocal()
        {
            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();

            var remoteBranchesNotBehindLocal =
                verbotBranchesWithRemote
                    .Where(b => b.RemoteTarget != null)
                    .Where(b => !GitRepository.IsAncestor(b.RemoteTarget, b.Target))
                    .ToList();

            if (!remoteBranchesNotBehindLocal.Any()) return;

            foreach (var branch in remoteBranchesNotBehindLocal)
            {
                Trace.TraceError(
                    $"Remote branch {branch.Name} at {branch.RemoteTarget} not behind local at {branch.Target}");
            }

            throw new UserException("Remote branch(es) not behind local");
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
                if (releases.Count() <= 1) continue;
                var sha1 = releases.Key.Sha1;
                var releaseNames = string.Join(", ", releases.Select(t => t.Version));
                Trace.TraceError($"Multiple releases on commit {sha1}: {releaseNames}");
                passed = false;
            }

            if (!passed)
            {
                throw new UserException("Commit(s) with multiple releases");
            }
        }


        void CheckForMissingReleaseTags()
        {
            if (!ReleasesDescending.Any()) return;

            var allVersions = new HashSet<SemVersion>(ReleasesDescending.Select(r => r.Version));
            var missingVersions = new List<SemVersion>();

            var latestVersion = allVersions.Max();
            for (var major = 1; major <= latestVersion.Major; major++)
            {
                var v = new SemVersion(major, 0, 0);
                if (!allVersions.Contains(v))
                {
                    missingVersions.Add(v);
                }
            }

            var latestMinorVersions =
                allVersions
                    .GroupBy(v => v.Change(minor: 0, patch: 0))
                    .Select(g => g.Max())
                    .OrderBy(v => v);
            foreach (var latestMinorVersion in latestMinorVersions)
            {
                for (var minor = 1; minor <= latestMinorVersion.Minor; minor++)
                {
                    var v = latestMinorVersion.Change(minor: minor, patch: 0);
                    if (!allVersions.Contains(v))
                    {
                        missingVersions.Add(v);
                    }
                }
            }

            var latestPatchVersions =
                allVersions
                    .GroupBy(v => v.Change(patch: 0))
                    .Select(g => g.Max())
                    .OrderBy(v => v);
            foreach (var latestPatchVersion in latestPatchVersions)
            {
                for (var patch = 1; patch < latestPatchVersion.Patch; patch++)
                {
                    var v = latestPatchVersion.Change(patch: patch);
                    if (!allVersions.Contains(v))
                    {
                        missingVersions.Add(v);
                    }
                }
            }

            if (missingVersions.Any())
            {
                foreach (var missingVersion in missingVersions.OrderBy(v => v))
                {
                    Trace.TraceError($"Missing release {missingVersion}");
                }

                throw new UserException("Missing release(s)");
            }
        }


        void CheckReleaseLineage()
        {
            var passed =
                ReleasesAscending.Aggregate(true, (result, release) =>
                    result &=
                        release.IsMajor ? CheckMajorReleaseLineage(release)
                        : release.IsMinor ? CheckMinorReleaseLineage(release)
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
