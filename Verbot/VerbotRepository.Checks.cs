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
            var releaseTags = FindReleaseTags();
            CheckForVersionLocations();
            CheckForConflictingVersions();
            CheckForMissingVersions();
            CheckForMissingReleaseTags(releaseTags);
            CheckReleaseLineage(releaseTags);
        }


        public void CheckRemote()
        {
            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();
            var verbotTagsWithRemote = FindReleaseTagsWithRemote();

            CheckForRemoteBranchesAtUnknownCommits(verbotBranchesWithRemote);
            CheckForRemoteBranchesNotBehindLocal(verbotBranchesWithRemote);
            CheckForIncorrectRemoteTags(verbotTagsWithRemote);
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
            var masterBranches = FindMasterBranches();
            if (masterBranches.Any(mb => mb.Name == "master") && masterBranches.First().Name != "master")
                throw new UserException("Expected master branch to be tracking the latest version");
        }


        void CheckOnCorrectMasterBranchForVersion()
        {
            var minorVersion = ReadFromVersionLocations().Change(null, null, 0, "", "");
            var expectedCurrentBranch =
                FindMasterBranches()
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
            var masterBranches = FindMasterBranches();
            if (masterBranches.Count == 0) return;
            var newMinorVersion = newVersion.Change(null, null, 0, "", "");
            if (newMinorVersion > masterBranches.First().Version && GitRepository.GetBranch() != "master")
                throw new UserException("Must be on master branch to advance to latest version");
        }


        void CheckForIncorrectRemoteTags(IEnumerable<GitRefWithRemote> verbotTagsWithRemote)
        {
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


        void CheckForRemoteBranchesAtUnknownCommits(IEnumerable<GitRefWithRemote> verbotBranchesWithRemote)
        {
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


        void CheckForRemoteBranchesNotBehindLocal(IEnumerable<GitRefWithRemote> verbotBranchesWithRemote)
        {
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


        void CheckForMissingReleaseTags(IEnumerable<ReleaseTagInfo> releaseTags)
        {
            var allVersions = new HashSet<SemVersion>(releaseTags.Select(tag => tag.Version));
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


        void CheckReleaseLineage(IEnumerable<ReleaseTagInfo> releaseTags)
        {
            var passed =
                CheckMajorReleaseLineage(releaseTags) &
                CheckMinorReleaseLineage(releaseTags) &
                CheckPatchReleaseLineage(releaseTags);

            if (!passed)
            {
                throw new UserException("Invalid release lineage");
            }
        }

        bool CheckMajorReleaseLineage(IEnumerable<ReleaseTagInfo> releaseTags)
        {
            var passed = true;

            var majorReleaseTags = releaseTags.Where(t => t.Version.Minor == 0 && t.Version.Patch == 0);
            var lookup = majorReleaseTags.ToDictionary(t => t.Version);
            foreach (var tag in majorReleaseTags)
            {
                var version = tag.Version;
                var previousVersion = version.Change(major: version.Major - 1);
                var previousTag = previousVersion.Major > 0 ? lookup[previousVersion] : null;

                if (version.Major > 1 && !tag.Target.DescendsFrom(previousTag.Target))
                {
                    Trace.TraceError($"Release {tag.Name} does not descend from {previousTag.Name}");
                    passed = false;
                    continue;
                }

                var commitsSincePrevious = tag.Target.ListCommitsFrom(previousTag?.Target).ToList();

                var breakingChange = commitsSincePrevious.FirstOrDefault(c => c.IsBreaking);
                if (breakingChange == null)
                {
                    Trace.TraceWarning($"No breaking change(s) between {previousVersion} and {version}");
                    continue;
                }
            }

            return passed;
        }

        bool CheckMinorReleaseLineage(IEnumerable<ReleaseTagInfo> releaseTags)
        {
            var passed = true;

            var minorReleaseTags = releaseTags.Where(t => t.Version.Patch == 0);
            var lookup = minorReleaseTags.ToDictionary(t => t.Version);
            foreach (var tag in minorReleaseTags)
            {
                var version = tag.Version;
                if (version.Minor == 0) continue;

                var previousVersion = version.Change(minor: version.Minor - 1);
                var previousTag = lookup[previousVersion];

                if (!tag.Target.DescendsFrom(previousTag.Target))
                {
                    Trace.TraceError($"Release {tag.Name} does not descend from {previousTag.Name}");
                    passed = false;
                    continue;
                }

                var commitsSincePrevious = tag.Target.ListCommitsFrom(previousTag.Target).ToList();

                var breakingChange = commitsSincePrevious.FirstOrDefault(c => c.IsBreaking);
                if (breakingChange != null)
                {
                    Trace.TraceWarning($"Breaking change(s) between {previousVersion} and {version}");
                    Trace.TraceWarning(breakingChange.Sha1);
                    Trace.TraceWarning(breakingChange.Message);
                }

                var featureChange = commitsSincePrevious.FirstOrDefault(c => c.IsFeature);
                if (featureChange == null)
                {
                    Trace.TraceWarning($"No feature change(s) between {previousVersion} and {version}");
                }
            }

            return passed;
        }

        bool CheckPatchReleaseLineage(IEnumerable<ReleaseTagInfo> releaseTags)
        {
            var passed = true;

            var lookup = releaseTags.ToDictionary(t => t.Version);
            foreach (var tag in releaseTags)
            {
                var version = tag.Version;
                if (version.Patch == 0) continue;

                var previousVersion = version.Change(patch: version.Patch - 1);
                var previousTag = lookup[previousVersion];

                if (!tag.Target.DescendsFrom(previousTag.Target))
                {
                    Trace.TraceError($"Release {tag.Name} does not descend from {previousTag.Name}");
                    passed = false;
                    continue;
                }

                var commitsSincePrevious = tag.Target.ListCommitsFrom(previousTag.Target).ToList();

                var breakingChange = commitsSincePrevious.FirstOrDefault(c => c.IsBreaking);
                if (breakingChange != null)
                {
                    Trace.TraceWarning($"Breaking change(s) between {previousVersion} and {version}");
                    Trace.TraceWarning(breakingChange.Sha1);
                    Trace.TraceWarning(breakingChange.Message);
                }

                var featureChange = commitsSincePrevious.FirstOrDefault(c => c.IsFeature);
                if (featureChange != null)
                {
                    Trace.TraceWarning($"Feature change(s) between {previousVersion} and {version}");
                    Trace.TraceWarning(featureChange.Sha1);
                    Trace.TraceWarning(featureChange.Message);
                }
            }

            return passed;
        }

    }
}
