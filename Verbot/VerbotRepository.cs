using System;
using System.Collections.Generic;
using System.Linq;
using MacroExceptions;
using MacroGit;
using MacroGuards;
using MacroSemver;
using MacroSln;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MacroSystem;

namespace Verbot
{
    class VerbotRepository
    {

        static readonly SemVersion DefaultVersion = new SemVersion(9999, 0, 0, "alpha");


        readonly GitRepository GitRepository;
        readonly VisualStudioSolution Solution;


        public VerbotRepository(GitRepository gitRepository)
        {
            Guard.NotNull(gitRepository, nameof(gitRepository));

            var solution = VisualStudioSolution.Find(gitRepository.Path);
            if (solution == null)
            {
                throw new UserException("No Visual Studio solution found in repository");
            }

            GitRepository = gitRepository;
            Solution = solution;
        }


        public SemVersion CalculateVersion(bool verbose)
        {
            return CalculateVersion(new GitCommitName("HEAD"), verbose);
        }


        public SemVersion CalculateReleaseVersion(bool verbose)
        {
            return CalculateReleaseVersion(new GitCommitName("HEAD"), verbose);
        }


        public SemVersion CalculatePrereleaseVersion(bool verbose)
        {
            return CalculatePrereleaseVersion(new GitCommitName("HEAD"), verbose);
        }


        public SemVersion WriteVersion(bool verbose)
        {
            var version = CalculateVersion(verbose);
            WriteToVersionLocations(version);
            return version;
        }


        public SemVersion WriteReleaseVersion(bool verbose)
        {
            var version = CalculateReleaseVersion(verbose);
            WriteToVersionLocations(version);
            return version;
        }


        public SemVersion WritePrereleaseVersion(bool verbose)
        {
            var version = CalculatePrereleaseVersion(verbose);
            WriteToVersionLocations(version);
            return version;
        }


        public SemVersion ReadFromVersionLocations()
        {
            CheckLocal();

            var locations = FindVersionLocations();

            var version =
                locations
                    .Select(l => l.GetVersion())
                    .Where(v => v != null)
                    .Distinct()
                    .SingleOrDefault();

            if (version == null)
            {
                throw new UserException("No version recorded in repository");
            }

            return version;
        }


        public void WriteDefaultVersion()
        {
            WriteToVersionLocations(DefaultVersion);
        }


        public void Release()
        {
            var version = CalculateReleaseVersion(false);

            var releaseTags = FindReleaseTags();
            if (releaseTags.Any(tag => tag.Version == version))
            {
                throw new UserException($"Version {version} has already been released");
            }

            Trace.TraceInformation($"Tagging {version}");
            GitRepository.CreateTag(new GitCommitName(version));
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
            GitRepository.CreateTag(new GitCommitName(version.ToString()));

            // Set MAJOR.MINOR-latest branch
            var majorMinorLatestBranch = FormattableString.Invariant($"{version.Major}.{version.Minor}-latest");
            Trace.TraceInformation(FormattableString.Invariant($"Setting branch {majorMinorLatestBranch}"));
            GitRepository.CreateOrMoveBranch(new GitCommitName(majorMinorLatestBranch));

            // Set MAJOR-latest branch
            var minorVersion = version.Change(null, null, 0, "", "");
            var latestMajorMinorLatestBranch =
                FindMajorMinorLatestBranches().Where(b => b.Version.Major == version.Major).First();
            var isLatestMajorMinorLatestBranch = minorVersion >= latestMajorMinorLatestBranch.Version;
            if (isLatestMajorMinorLatestBranch)
            {
                var majorLatestBranch = FormattableString.Invariant($"{version.Major}-latest");
                Trace.TraceInformation(FormattableString.Invariant($"Setting branch {majorLatestBranch}"));
                GitRepository.CreateOrMoveBranch(new GitCommitName(majorLatestBranch));
            }

            // Set latest branch
            var majorVersion = version.Change(null, 0, 0, "", "");
            var latestMajorLatestBranch = FindMajorLatestBranches().First();
            var isLatestMajorLatestBranch = majorVersion >= latestMajorLatestBranch.Version;
            if (isLatestMajorMinorLatestBranch && isLatestMajorLatestBranch)
            {
                Trace.TraceInformation(FormattableString.Invariant($"Setting branch latest"));
                GitRepository.CreateOrMoveBranch(new GitCommitName("latest"));
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
            var masterBranches = FindMasterBranches();
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

                    if (masterBranches.Any(mb => mb.Name != "master" && mb.Version == newBranchVersion))
                        throw new UserException(FormattableString.Invariant(
                            $"A -master branch tracking {newBranchVersion.Major}.{newBranchVersion.Minor} already exists"));

                    var newBranch = new GitCommitName(FormattableString.Invariant(
                        $"{newBranchVersion.Major}.{newBranchVersion.Minor}-master"));

                    Trace.TraceInformation("Creating branch " + newBranch);
                    GitRepository.CreateBranch(newBranch);
                }
                else
                {
                    var newBranchVersion = nextMinorVersion;

                    if (masterBranches.Any(mb => mb.Version == newBranchVersion))
                        throw new UserException(FormattableString.Invariant(
                            $"A master branch tracking {newBranchVersion.Major}.{newBranchVersion.Minor} already exists"));

                    var newBranch = new GitCommitName(FormattableString.Invariant(
                        $"{newBranchVersion.Major}.{newBranchVersion.Minor}-master"));

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
            var verbotTagsWithRemote = GetVerbotTagsWithRemote();

            CheckForRemoteBranchesAtUnknownCommits(verbotBranchesWithRemote);
            CheckForRemoteBranchesNotBehindLocal(verbotBranchesWithRemote);
            CheckForIncorrectRemoteTags(verbotTagsWithRemote);

            var branchesToPush = verbotBranchesWithRemote.Where(b => b.RemoteId != b.LocalId);
            var tagsToPush = verbotTagsWithRemote.Where(b => b.RemoteId != b.LocalId);
            var refsToPush = branchesToPush.Concat(tagsToPush);

            if (!refsToPush.Any())
            {
                Trace.TraceInformation("All remote version branches and tags already up-to-date");
                return;
            }

            GitRepository.Push(refsToPush.Select(r => r.Name), dryRun: dryRun, echoOutput: true);
        }


        public void CheckLocal()
        {
            CheckForVersionLocations();
            CheckForConflictingVersions();
            CheckForMissingVersions();
        }


        public void CheckRemote()
        {
            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();
            var verbotTagsWithRemote = GetVerbotTagsWithRemote();

            CheckForRemoteBranchesAtUnknownCommits(verbotBranchesWithRemote);
            CheckForRemoteBranchesNotBehindLocal(verbotBranchesWithRemote);
            CheckForIncorrectRemoteTags(verbotTagsWithRemote);
        }


        void WriteToVersionLocations(SemVersion version)
        {
            Guard.NotNull(version, nameof(version));

            CheckForVersionLocations();

            foreach (var location in FindVersionLocations())
            {
                location.SetVersion(version);
            }
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
                    .Where(t => t.RemoteId != null)
                    .Where(t => t.RemoteId != t.LocalId)
                    .ToList();

            if (!incorrectRemoteTags.Any()) return;

            foreach (var tag in incorrectRemoteTags)
            {
                Trace.TraceError($"Remote tag {tag.Name} at {tag.RemoteId} local {tag.LocalId}");
            }

            throw new UserException("Incorrect remote tag(s) found");
        }


        void CheckForRemoteBranchesAtUnknownCommits(IEnumerable<GitRefWithRemote> verbotBranchesWithRemote)
        {
            var remoteBranchesAtUnknownCommits =
                verbotBranchesWithRemote
                    .Where(b => b.RemoteId != null)
                    .Where(b => !GitRepository.Exists(b.RemoteId))
                    .ToList();

            if (!remoteBranchesAtUnknownCommits.Any()) return;

            foreach (var branch in remoteBranchesAtUnknownCommits)
            {
                Trace.TraceError($"Remote branch {branch.Name} at unknown commit {branch.RemoteId}");
            }

            throw new UserException("Remote branch(es) at unknown commits");
        }


        void CheckForRemoteBranchesNotBehindLocal(IEnumerable<GitRefWithRemote> verbotBranchesWithRemote)
        {
            var remoteBranchesNotBehindLocal =
                verbotBranchesWithRemote
                    .Where(b => b.RemoteId != null)
                    .Where(b => !GitRepository.IsAncestor(b.RemoteId, b.LocalId))
                    .ToList();

            if (!remoteBranchesNotBehindLocal.Any()) return;

            foreach (var branch in remoteBranchesNotBehindLocal)
            {
                Trace.TraceError(
                    $"Remote branch {branch.Name} at {branch.RemoteId} not behind local at {branch.LocalId}");
            }

            throw new UserException("Remote branch(es) not behind local");
        }


        IEnumerable<GitRefWithRemote> GetVerbotTagsWithRemote()
        {
            var verbotTags = FindReleaseTags();
            var remoteTagsLookup = GitRepository.GetRemoteTags().ToDictionary(t => t.Name, t => t.Id);

            GitCommitName LookupRemoteId(GitCommitName name) =>
                remoteTagsLookup.TryGetValue(name, out var id) ? id : null;

            return
                verbotTags
                    .Select(t => new GitRefWithRemote(t.Name, t.Id, LookupRemoteId(t.Name)))
                    .ToList();
        }


        IEnumerable<GitRefWithRemote> GetVerbotBranchesWithRemote()
        {
            var verbotBranches = GetVerbotBranches();
            var remoteBranchesLookup = GitRepository.GetRemoteBranches().ToDictionary(b => b.Name, b => b.Id);

            GitCommitName LookupRemoteId(GitCommitName name) =>
                remoteBranchesLookup.TryGetValue(name, out var id) ? id : null;

            return
                verbotBranches
                    .Select(t => new GitRefWithRemote(t.Name, t.Id, LookupRemoteId(t.Name)))
                    .ToList();
        }


        public SemVersion CalculateReleaseVersion(GitCommitName name, bool verbose)
        {
            return
                CalculateVersion(name, verbose)
                    .Change(prerelease: "", build: "");
        }


        SemVersion CalculateVersion(GitCommitName name, bool verbose)
        {
            var commitId = GitRepository.GetCommitId(name);
            var releaseTags = FindReleaseTags().ToList();

            var versionFromTag =
                releaseTags
                    .Where(tag => tag.Id == commitId)
                    .Select(tag => tag.Version)
                    .SingleOrDefault();

            return
                versionFromTag ??
                CalculatePrereleaseVersion(name, releaseTags, verbose);
        }


        SemVersion CalculatePrereleaseVersion(GitCommitName name, bool verbose)
        {
            return CalculatePrereleaseVersion(name, FindReleaseTags(), verbose);
        }


        SemVersion CalculatePrereleaseVersion(GitCommitName name, IEnumerable<ReleaseTagInfo> releaseTags, bool verbose)
        {
            SemVersion version;

            void TraceStep(string description)
            {
                if (!verbose) return;
                Trace.TraceInformation($"{version} ({description})");
            }

            var commitId = GitRepository.GetCommitId(name);

            var mostRecentReleaseTag =
                releaseTags
                    .Where(t => t.Id != commitId)
                    .FirstOrDefault(t => GitRepository.IsAncestor(t.Name, name));

            if (mostRecentReleaseTag != null)
            {
                version = mostRecentReleaseTag.Version;
                TraceStep($"Previous release tag at {mostRecentReleaseTag.Id}");
            }
            else
            {
                version = new SemVersion(0, 0, 0);
                TraceStep($"No previous release tags");
            }

            var commitsSincePreviousRelease = GitRepository.ListCommits(mostRecentReleaseTag?.Name, name).ToList();
            string firstMajorChangeId = null;
            string firstMinorChangeId = null;
            foreach (var id in commitsSincePreviousRelease)
            {
                var message = GitRepository.GetCommitMessage(id);
                var lines = StringExtensions.SplitLines(message).Select(line => line.Trim()).ToList();
                if (lines.Any(line => Regex.IsMatch(line, @"^\+semver:\s?(breaking|major)$")))
                {
                    firstMajorChangeId = firstMajorChangeId ?? id;
                    break;
                }
                if (lines.Any(line => Regex.IsMatch(line, @"^\+semver:\s?(feature|minor)$")))
                {
                    firstMinorChangeId = firstMinorChangeId ?? id;
                }
            }

            if (firstMajorChangeId != null)
            {
                version = version.Change(major: version.Major + 1, minor: 0, patch: 0);
                TraceStep($"Major +semver commit {firstMajorChangeId}");
            }
            else if (firstMinorChangeId != null)
            {
                version = version.Change(minor: version.Minor + 1, patch: 0);
                TraceStep($"Minor +semver commit {firstMinorChangeId}");
            }
            else
            {
                version = version.Change(patch: version.Patch + 1);
                TraceStep($"No major or minor +semver commit(s)");
            }

            version = version.Change(prerelease: "alpha");
            TraceStep($"Pre-release");

            var distance = commitsSincePreviousRelease.Count;
            version = version.Change(prerelease: $"{version.Prerelease}.{distance}");
            TraceStep($"Number of commit(s) since previous release");

            var committerDate = GitRepository.GetCommitterDate(name).ToUniversalTime();
            var committerDateIdentifier = committerDate.ToString("yyyyMMddTHHmmss");
            version = version.Change(prerelease: $"{version.Prerelease}.{committerDateIdentifier}");
            TraceStep($"Commit date");

            var shortHash = GitRepository.GetShortCommitId(name, 4);
            version = version.Change(prerelease: $"{version.Prerelease}.{shortHash}");
            TraceStep($"Short commit hash");

            return version;
        }


        IEnumerable<ReleaseTagInfo> FindReleaseTags()
        {
            return
                GitRepository.GetTags()
                    .Where(t => IsReleaseVersionNumber(t.Name))
                    .Select(t => new ReleaseTagInfo(t.Name, SemVersion.Parse(t.Name), t.Id))
                    .OrderByDescending(t => t.Version)
                    .ToList();
        }


        IEnumerable<GitRef> GetVerbotBranches()
        {
            return
                GitRepository.GetBranches()
                    .Where(b =>
                        MasterBranchInfo.IsMasterBranchName(b.Name) ||
                        b.Name == "latest" ||
                        MajorLatestBranchInfo.IsMajorLatestBranchName(b.Name) ||
                        MajorMinorLatestBranchInfo.IsMajorMinorLatestBranchName(b.Name))
                    .ToList();
        }


        bool IsReleaseVersionNumber(string value)
        {
            return Regex.IsMatch(value, @"^\d+\.\d+\.\d+$");
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


        ICollection<VersionLocation> FindVersionLocations()
        {
            return
                FindProjects()
                    .Select(p => new VersionLocation(p))
                    .ToList();
        }


        ICollection<VisualStudioProject> FindProjects()
        {
            return
                // All projects
                Solution.ProjectReferences
                    // ...that are C# projects
                    .Where(pr =>
                        pr.TypeId == VisualStudioProjectTypeIds.CSharp ||
                        pr.TypeId == VisualStudioProjectTypeIds.CSharpNew)
                    // ..."local" to the solution
                    .Where(pr => !pr.Location.StartsWith("..", StringComparison.Ordinal))
                    .Select(pr => pr.GetProject())
                    .ToList();
        }


        /// <summary>
        /// Find information about all 'master' and 'MAJOR.MINOR-master' branches, in decreasing order of tracked minor
        /// version
        /// </summary>
        ///
        IList<MasterBranchInfo> FindMasterBranches()
        {
            return
                GitRepository.GetBranches()
                    .Where(b => MasterBranchInfo.IsMasterBranchName(b.Name))
                    .Select(b => new MasterBranchInfo(this, b.Name))
                    .OrderByDescending(b => b.Version)
                    .ToList();
        }


        /// <summary>
        /// Find information about all 'MAJOR-latest' branches, in decreasing version order
        /// </summary>
        ///
        IList<MajorLatestBranchInfo> FindMajorLatestBranches()
        {
            return
                GitRepository.GetBranches()
                    .Where(b => MajorLatestBranchInfo.IsMajorLatestBranchName(b.Name))
                    .Select(b => new MajorLatestBranchInfo(this, b.Name))
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
                    .Select(b => new MajorMinorLatestBranchInfo(this, b.Name))
                    .OrderByDescending(b => b.Version)
                    .ToList();
        }


        class GitRefWithRemote
        {
            public GitRefWithRemote(GitCommitName name, GitCommitName localId, GitCommitName remoteId)
            {
                Name = name;
                LocalId = localId;
                RemoteId = remoteId;
            }

            public GitCommitName Name { get; }
            public GitCommitName LocalId { get; }
            public GitCommitName RemoteId { get; }
        }


        class ReleaseTagInfo
        {
            public ReleaseTagInfo(GitCommitName name, SemVersion version, GitCommitName id)
            {
                Name = name;
                Version = version;
                Id = id;
            }

            public GitCommitName Name { get; }
            public SemVersion Version { get; }
            public GitCommitName Id { get; }
        }

    }
}
