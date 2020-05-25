using System.Collections.Generic;
using System.Linq;
using MacroGit;
using MacroSemver;

namespace Verbot
{
    partial class VerbotRepository
    {

        public SemVersion CalculateVersion() =>
            CalculateVersion(GetHeadCommit());


        public SemVersion CalculateVersion(VerbotCommitInfo commit) =>
            CalculateVersion(commit, FindReleaseTags());


        SemVersion CalculateVersion(VerbotCommitInfo commit, IEnumerable<ReleaseTagInfo> releaseTags) =>
            releaseTags
                .Where(tag => tag.Target == commit)
                .Select(tag => tag.Version)
                .SingleOrDefault() ??
            CalculatePrereleaseVersion(commit, releaseTags);


        public SemVersion CalculateReleaseVersion() =>
            CalculateReleaseVersion(GetHeadCommit());


        public SemVersion CalculateReleaseVersion(VerbotCommitInfo commit) =>
            CalculateVersion(commit)
                .Change(prerelease: "", build: "");


        public SemVersion CalculatePrereleaseVersion() =>
            CalculatePrereleaseVersion(GetHeadCommit());


        public SemVersion CalculatePrereleaseVersion(VerbotCommitInfo commit) =>
            CalculatePrereleaseVersion(commit, FindReleaseTags());


        SemVersion CalculatePrereleaseVersion(
            VerbotCommitInfo commit,
            IEnumerable<ReleaseTagInfo> releaseTags)
        {
            SemVersion version;

            void TraceStep(string description) =>
                TraceVerbose($"{version} ({description})");

            var mostRecentReleaseTag =
                releaseTags
                    .Where(t => t.Target != commit)
                    .FirstOrDefault(t => GitRepository.IsAncestor(t.Target.Sha1, commit.Sha1));

            if (mostRecentReleaseTag != null)
            {
                version = mostRecentReleaseTag.Version;
                TraceStep($"Previous release tag at {mostRecentReleaseTag.Target.Sha1}");
            }
            else
            {
                version = new SemVersion(0, 0, 0);
                TraceStep($"No previous release tags");
            }

            var commitsSincePreviousRelease =
                GetCommits(GitRepository.ListCommits(mostRecentReleaseTag?.Name, commit.Sha1)).ToList();
            var firstMajorChange = (VerbotCommitInfo)null;
            var firstMinorChange = (VerbotCommitInfo)null;
            foreach (var c in commitsSincePreviousRelease)
            {
                if (c.IsBreaking)
                {
                    firstMajorChange = firstMajorChange ?? c;
                    break;
                }

                if (c.IsFeature)
                {
                    firstMinorChange = firstMinorChange ?? c;
                }
            }

            if (firstMajorChange != null)
            {
                version = version.Change(major: version.Major + 1, minor: 0, patch: 0);
                TraceStep($"Major +semver commit {firstMajorChange.Sha1}");
            }
            else if (firstMinorChange != null)
            {
                version = version.Change(minor: version.Minor + 1, patch: 0);
                TraceStep($"Minor +semver commit {firstMinorChange.Sha1}");
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

            var committerDate = GitRepository.GetCommitterDate(commit.Sha1).ToUniversalTime();
            var committerDateIdentifier = committerDate.ToString("yyyyMMddTHHmmss");
            version = version.Change(prerelease: $"{version.Prerelease}.{committerDateIdentifier}");
            TraceStep($"Commit date");

            var shortHash = GitRepository.GetShortCommitId(commit.Sha1, 4);
            version = version.Change(prerelease: $"{version.Prerelease}.{shortHash}");
            TraceStep($"Short commit hash");

            return version;
        }

    }
}
