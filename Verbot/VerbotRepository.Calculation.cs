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


        public SemVersion CalculateReleaseVersion() =>
            CalculateReleaseVersion(GetHeadCommit());


        public SemVersion CalculatePrereleaseVersion() =>
            CalculatePrereleaseVersion(GetHeadCommit());


        public SemVersion CalculateVersion(CommitInfo commit) =>
            ReleasesDescending
                .Where(tag => tag.Commit == commit)
                .Select(tag => tag.Version)
                .SingleOrDefault() ??
            CalculatePrereleaseVersion(commit);


        public SemVersion CalculateReleaseVersion(CommitInfo commit) =>
            CalculateVersion(commit)
                .Change(prerelease: "", build: "");


        SemVersion CalculatePrereleaseVersion(CommitInfo commit)
        {
            SemVersion version;

            void TraceStep(string description) =>
                TraceVerbose($"{version} ({description})");

            var mostRecentReleaseTag =
                ReleasesDescending
                    .Where(t => t.Commit != commit)
                    .FirstOrDefault(t => GitRepository.IsAncestor(t.Commit.Sha1, commit.Sha1));

            if (mostRecentReleaseTag != null)
            {
                version = mostRecentReleaseTag.Version;
                TraceStep($"Previous release tag at {mostRecentReleaseTag.Commit.Sha1}");
            }
            else
            {
                version = new SemVersion(0, 0, 0);
                TraceStep($"No previous release tags");
            }

            var commitsSincePreviousRelease =
                GetCommits(GitRepository.ListCommits(mostRecentReleaseTag?.Commit.Sha1, commit.Sha1)).ToList();
            var firstMajorChange = (CommitInfo)null;
            var firstMinorChange = (CommitInfo)null;
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
