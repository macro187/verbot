using System;
using System.Collections.Generic;
using System.Linq;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    class ReleaseInfo
    {

        public ReleaseInfo(VerbotRepository repository, SemVersion version, VerbotCommitInfo commit, GitRef tag)
        {
            Guard.NotNull(repository, nameof(repository));
            Guard.NotNull(tag, nameof(tag));
            if (!tag.IsTag)
            {
                throw new ArgumentException("Not a tag", nameof(tag));
            }
            Guard.NotNull(version, nameof(version));
            if (version.Prerelease != "" || version.Build != "")
            {
                throw new ArgumentException("Not a release version", nameof(version));
            }
            Guard.NotNull(commit, nameof(commit));

            Repository = repository;
            Version = version;
            Commit = commit;
            Tag = tag;
        }


        VerbotRepository Repository { get; }
        public SemVersion Version { get; }
        public VerbotCommitInfo Commit { get; }
        public GitRef Tag { get; }


        public bool IsMajor =>
            Version.Minor == 0 && Version.Patch == 0;


        public bool IsMinor =>
            Version.Patch == 0;


        public ReleaseInfo PreviousMajor =>
            IsMajor
                ? Repository.FindRelease(new SemVersion(Version.Major - 1, 0, 0))
                : Repository.FindRelease(Version.Change(minor: 0, patch: 0));


        public ReleaseInfo PreviousMinorAncestor =>
            CommitsSincePreviousMajor
                .Reverse()
                .Skip(1)
                .Select(c => Repository.GetReleases(c).OrderBy(r => r.Version).LastOrDefault())
                .Where(r => r != null)
                .Where(r => r.IsMinor)
                .FirstOrDefault()
            ?? PreviousMajor;


        public ReleaseInfo PreviousMinorNumeric =>
            Repository.ReleasesDescending
                .Where(r => r.Version < Version)
                .Where(r => r.IsMinor)
                .FirstOrDefault();


        public ReleaseInfo PreviousAncestor =>
            CommitsSincePreviousMajor
                .Reverse()
                .Skip(1)
                .Select(c => Repository.GetReleases(c).OrderBy(r => r.Version).LastOrDefault())
                .Where(r => r != null)
                .FirstOrDefault()
            ?? PreviousMajor;


        public ReleaseInfo PreviousNumeric =>
            Repository.ReleasesDescending
                .Where(r => r.Version < Version)
                .FirstOrDefault();


        public IEnumerable<VerbotCommitInfo> CommitsSincePreviousMajor =>
            Repository.GetCommitsBetween(PreviousMajor?.Commit.Sha1, Commit.Sha1);


        public IEnumerable<VerbotCommitInfo> CommitsSincePreviousMinorAncestor =>
            Repository.GetCommitsBetween(PreviousMinorAncestor?.Commit.Sha1, Commit.Sha1);


        public IEnumerable<VerbotCommitInfo> CommitsSincePreviousAncestor =>
            Repository.GetCommitsBetween(PreviousAncestor?.Commit.Sha1, Commit.Sha1);

    }
}
