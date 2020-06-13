using System.Linq;
using MacroExceptions;
using System.Diagnostics;
using MacroGit;
using System;
using MacroSemver;
using System.Collections.Generic;
using MacroGuards;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Verbot
{
    partial class VerbotRepository
    {

        void OldRelease()
        {
            CheckLocal();

            CheckNoUncommittedChanges();
            CheckMasterBranchIsTrackingHighestVersion();
            CheckVersionIsMasterPrerelease();
            CheckOnCorrectMasterBranchForVersion();
            CheckVersionHasNotBeenReleased();

            // Set release version and commit
            var version = ReadFromVersionLocations().Change(null, null, null, "", "");
            Trace.TraceInformation(FormattableString.Invariant($"Setting version to {version} and committing"));
            WriteToVersionLocations(version);
            GitRepository.StageChanges();
            GitRepository.Commit(FormattableString.Invariant($"Release version {version.ToString()}"));

            // Tag MAJOR.MINOR.PATCH
            Trace.TraceInformation("Tagging " + version.ToString());
            GitRepository.CreateTag(new GitRefNameComponent(version.ToString()));

            // Set MAJOR.MINOR-latest branch
            var majorMinorLatestBranch = new GitRefNameComponent($"{version.Major}.{version.Minor}-latest");
            Trace.TraceInformation(FormattableString.Invariant($"Setting branch {majorMinorLatestBranch}"));
            GitRepository.CreateOrMoveBranch(majorMinorLatestBranch);

            // Set MAJOR-latest branch
            var minorVersion = version.Change(null, null, 0, "", "");
            var latestMajorMinorLatestBranch =
                FindMajorMinorLatestBranches().Where(b => b.Version.Major == version.Major).First();
            var isLatestMajorMinorLatestBranch = minorVersion >= latestMajorMinorLatestBranch.Version;
            if (isLatestMajorMinorLatestBranch)
            {
                var majorLatestBranch = new GitRefNameComponent(FormattableString.Invariant($"{version.Major}-latest"));
                Trace.TraceInformation(FormattableString.Invariant($"Setting branch {majorLatestBranch}"));
                GitRepository.CreateOrMoveBranch(majorLatestBranch);
            }

            // Set latest branch
            var majorVersion = version.Change(null, 0, 0, "", "");
            var latestMajorLatestBranch = FindMajorLatestBranches().First();
            var isLatestMajorLatestBranch = majorVersion >= latestMajorLatestBranch.Version;
            if (isLatestMajorMinorLatestBranch && isLatestMajorLatestBranch)
            {
                Trace.TraceInformation(FormattableString.Invariant($"Setting branch latest"));
                GitRepository.CreateOrMoveBranch(new GitRefNameComponent("latest"));
            }

            // Increment to next patch version
            IncrementVersion(false, false);
        }


        public void IncrementVersion(bool major, bool minor)
        {
            CheckLocal();

            CheckNoUncommittedChanges();
            CheckMasterBranchIsTrackingHighestVersion();
            CheckVersionIsReleaseOrMasterPrerelease();
            CheckOnCorrectMasterBranchForVersion();

            var currentVersion = ReadFromVersionLocations();
            var currentMinorVersion = currentVersion.Change(null, null, 0, "", "");
            var currentBranch = GitRepository.GetBranch();
            var onMaster = (currentBranch == "master");
            var nextVersion = CalculateNextVersion(major, minor);
            var nextMinorVersion = nextVersion.Change(null, null, 0, "", "");
            CheckNotAdvancingToLatestVersionOnNonMasterBranch(nextVersion);
            CheckNotSkippingRelease(major, minor);

            // If incrementing major or minor, create and (if necessary) switch to new MAJOR.MINOR-master branch
            if (major || minor)
            {
                if (onMaster)
                {
                    var newBranchVersion = currentMinorVersion;

                    if (MasterBranches.Any(mb => mb.Name != "master" && mb.Series == newBranchVersion))
                        throw new UserException(FormattableString.Invariant(
                            $"A -master branch tracking {newBranchVersion.Major}.{newBranchVersion.Minor} already exists"));

                    var newBranch =
                        new GitRefNameComponent($"{newBranchVersion.Major}.{newBranchVersion.Minor}-master");

                    Trace.TraceInformation("Creating branch " + newBranch);
                    GitRepository.CreateBranch(newBranch);
                }
                else
                {
                    var newBranchVersion = nextMinorVersion;

                    if (MasterBranches.Any(mb => mb.Series == newBranchVersion))
                        throw new UserException(FormattableString.Invariant(
                            $"A master branch tracking {newBranchVersion.Major}.{newBranchVersion.Minor} already exists"));

                    var newBranch =
                        new GitRefNameComponent($"{newBranchVersion.Major}.{newBranchVersion.Minor}-master");

                    Trace.TraceInformation("Creating and switching to branch " + newBranch);
                    GitRepository.CreateBranch(newBranch);
                    GitRepository.Checkout(newBranch);
                }
            }

            // Set version and commit
            var incrementedComponent = major ? "major" : minor ? "minor" : "patch";
            Trace.TraceInformation(FormattableString.Invariant(
                $"Incrementing {incrementedComponent} version to {nextVersion} and committing"));
            WriteToVersionLocations(nextVersion);
            GitRepository.StageChanges();
            GitRepository.Commit(
                FormattableString.Invariant($"Increment {incrementedComponent} version to {nextVersion}"));
        }


        public void Push(bool dryRun)
        {
            CheckNoUncommittedChanges();

            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();
            var verbotTagsWithRemote = FindReleaseTagsWithRemote();

            CheckForRemoteBranchesAtUnknownCommits();
            CheckForRemoteBranchesNotBehindLocal();
            CheckForIncorrectRemoteTags();

            var branchesToPush = verbotBranchesWithRemote.Where(b => b.RemoteTargetSha1 != b.Target.Sha1);
            var tagsToPush = verbotTagsWithRemote.Where(b => b.RemoteTargetSha1 != b.Target.Sha1);
            var refsToPush = branchesToPush.Concat(tagsToPush);

            if (!refsToPush.Any())
            {
                Trace.TraceInformation("All remote version branches and tags already up-to-date");
                return;
            }

            GitRepository.Push(refsToPush.Select(r => r.FullName), dryRun: dryRun, echoOutput: true);
        }


        SemVersion CalculateNextVersion(bool major, bool minor)
        {
            var v = ReadFromVersionLocations().Change(null, null, null, "master", "");
            return
                major ?
                    v.Change(v.Major + 1, 0, 0, null, null)
                : minor ?
                    v.Change(null, v.Minor + 1, 0, null, null)
                :
                    v.Change(null, null, v.Patch + 1, null, null);
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
                    .Where(mb => mb.Series == minorVersion)
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
            if (newMinorVersion > MasterBranches.First().Series && GitRepository.GetBranch() != "master")
                throw new UserException("Must be on master branch to advance to latest version");
        }


        void CheckForIncorrectRemoteTags()
        {
            var verbotTagsWithRemote = FindReleaseTagsWithRemote();

            var incorrectRemoteTags =
                verbotTagsWithRemote
                    .Where(t => t.RemoteTargetSha1 != null)
                    .Where(t => t.RemoteTargetSha1 != t.Target.Sha1)
                    .ToList();

            if (!incorrectRemoteTags.Any()) return;

            foreach (var tag in incorrectRemoteTags)
            {
                Trace.TraceError($"Remote tag {tag.Name} at {tag.RemoteTargetSha1} local {tag.Target.Sha1}");
            }

            throw new UserException("Incorrect remote tag(s) found");
        }


        void CheckForRemoteBranchesAtUnknownCommits()
        {
            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();

            var remoteBranchesAtUnknownCommits =
                verbotBranchesWithRemote
                    .Where(b => b.RemoteTargetSha1 != null)
                    .Where(b => !GitRepository.Exists(b.RemoteTargetSha1))
                    .ToList();

            if (!remoteBranchesAtUnknownCommits.Any()) return;

            foreach (var branch in remoteBranchesAtUnknownCommits)
            {
                Trace.TraceError($"Remote branch {branch.Name} at unknown commit {branch.RemoteTargetSha1}");
            }

            throw new UserException("Remote branch(es) at unknown commits");
        }


        void CheckForRemoteBranchesNotBehindLocal()
        {
            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();

            var remoteBranchesNotBehindLocal =
                verbotBranchesWithRemote
                    .Where(b => b.RemoteTargetSha1 != null)
                    .Where(b => !GitRepository.IsAncestor(b.RemoteTargetSha1, b.Target.Sha1))
                    .ToList();

            if (!remoteBranchesNotBehindLocal.Any()) return;

            foreach (var branch in remoteBranchesNotBehindLocal)
            {
                Trace.TraceError(
                    $"Remote branch {branch.Name} at {branch.RemoteTargetSha1} not behind local at {branch.Target.Sha1}");
            }

            throw new UserException("Remote branch(es) not behind local");
        }


        IEnumerable<RefInfo> VerbotBranches =>
            Branches
                .Where(b =>
                    CalculateMasterBranchSeries(b) != null ||
                    b.Name == "latest" ||
                    MajorLatestBranchInfo.IsMajorLatestBranchName(b.Name) ||
                    MajorMinorLatestBranchInfo.IsMajorMinorLatestBranchName(b.Name))
                .ToList();


        public IEnumerable<GitRefWithRemote> GetVerbotBranchesWithRemote() =>
            GetRemoteInfo(VerbotBranches).ToList();


        /// <summary>
        /// Find information about all 'MAJOR-latest' branches, in decreasing version order
        /// </summary>
        ///
        IList<MajorLatestBranchInfo> FindMajorLatestBranches()
        {
            return
                GitRepository.GetBranches()
                    .Where(b => MajorLatestBranchInfo.IsMajorLatestBranchName(b.Name))
                    .Select(b => new MajorLatestBranchInfo(b))
                    .OrderByDescending(b => b.Version)
                    .ToList();
        }


        /// <summary>
        /// Find information about all 'MAJOR.MINOR-latest' branches, in decreasing version order
        /// </summary>
        ///
        IList<MajorMinorLatestBranchInfo> FindMajorMinorLatestBranches()
        {
            return
                GitRepository.GetBranches()
                    .Where(b => MajorMinorLatestBranchInfo.IsMajorMinorLatestBranchName(b.Name))
                    .Select(b => new MajorMinorLatestBranchInfo(b))
                    .OrderByDescending(b => b.Version)
                    .ToList();
        }

    }
}
