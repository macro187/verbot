using System.Linq;
using MacroExceptions;
using System.Diagnostics;
using MacroGit;
using System;
using MacroSemver;

namespace Verbot
{
    partial class VerbotRepository
    {

        public void Release()
        {
            var version = CalculateReleaseVersion();

            if (ReleasesDescending.Any(tag => tag.Version == version))
            {
                throw new UserException($"Version {version} has already been released");
            }

            Trace.TraceInformation($"Tagging {version}");
            GitRepository.CreateTag(new GitRefNameComponent(version));
        }


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

                    if (MasterBranches.Any(mb => mb.Name != "master" && mb.Version == newBranchVersion))
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

                    if (MasterBranches.Any(mb => mb.Version == newBranchVersion))
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

            var branchesToPush = verbotBranchesWithRemote.Where(b => b.RemoteTarget != b.Target);
            var tagsToPush = verbotTagsWithRemote.Where(b => b.RemoteTarget != b.Target);
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

    }
}
