using System.Collections.Generic;
using System.Linq;
using MacroGit;
using MacroSemver;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MacroSystem;

namespace Verbot
{
    partial class VerbotRepository
    {

        public SemVersion CalculateVersion(bool verbose)
        {
            return CalculateVersion(new GitRev("HEAD"), verbose);
        }


        public SemVersion CalculateVersion(GitRev rev, bool verbose)
        {
            var sha1 = GitRepository.GetCommitId(rev);
            var releaseTags = FindReleaseTags().ToList();

            var versionFromTag =
                releaseTags
                    .Where(tag => tag.Target == sha1)
                    .Select(tag => tag.Version)
                    .SingleOrDefault();

            return
                versionFromTag ??
                CalculatePrereleaseVersion(rev, releaseTags, verbose);
        }


        public SemVersion CalculateReleaseVersion(bool verbose)
        {
            return CalculateReleaseVersion(new GitRev("HEAD"), verbose);
        }


        public SemVersion CalculateReleaseVersion(GitRev rev, bool verbose)
        {
            return
                CalculateVersion(rev, verbose)
                    .Change(prerelease: "", build: "");
        }


        public SemVersion CalculatePrereleaseVersion(bool verbose)
        {
            return CalculatePrereleaseVersion(new GitRev("HEAD"), verbose);
        }


        public SemVersion CalculatePrereleaseVersion(GitRev rev, bool verbose)
        {
            return CalculatePrereleaseVersion(rev, FindReleaseTags(), verbose);
        }


        SemVersion CalculatePrereleaseVersion(GitRev rev, IEnumerable<ReleaseTagInfo> releaseTags, bool verbose)
        {
            SemVersion version;

            void TraceStep(string description)
            {
                if (!verbose) return;
                Trace.TraceInformation($"{version} ({description})");
            }

            var sha1 = GitRepository.GetCommitId(rev);

            var mostRecentReleaseTag =
                releaseTags
                    .Where(t => t.Target != sha1)
                    .FirstOrDefault(t => GitRepository.IsAncestor(t.Name, rev));

            if (mostRecentReleaseTag != null)
            {
                version = mostRecentReleaseTag.Version;
                TraceStep($"Previous release tag at {mostRecentReleaseTag.Target}");
            }
            else
            {
                version = new SemVersion(0, 0, 0);
                TraceStep($"No previous release tags");
            }

            var commitsSincePreviousRelease = GitRepository.ListCommits(mostRecentReleaseTag?.Name, rev).ToList();
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

            var committerDate = GitRepository.GetCommitterDate(rev).ToUniversalTime();
            var committerDateIdentifier = committerDate.ToString("yyyyMMddTHHmmss");
            version = version.Change(prerelease: $"{version.Prerelease}.{committerDateIdentifier}");
            TraceStep($"Commit date");

            var shortHash = GitRepository.GetShortCommitId(rev, 4);
            version = version.Change(prerelease: $"{version.Prerelease}.{shortHash}");
            TraceStep($"Short commit hash");

            return version;
        }

    }
}
