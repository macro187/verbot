using System;
using System.Collections.Generic;
using System.Linq;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    class ReleaseInfo
    {

        readonly VerbotRepository Repository;


        public ReleaseInfo(VerbotRepository repository, SemVersion version, RefInfo tag)
        {
            Guard.NotNull(repository, nameof(repository));

            Guard.NotNull(version, nameof(version));
            if (version.Prerelease != "" || version.Build != "")
            {
                throw new ArgumentException("Not a release version", nameof(version));
            }

            Guard.NotNull(tag, nameof(tag));
            if (!tag.IsTag)
            {
                throw new ArgumentException("Not a tag", nameof(tag));
            }

            Repository = repository;
            Version = version;
            Tag = tag;
        }


        public SemVersion Version { get; }
        public RefInfo Tag { get; }
        public CommitInfo Commit => Tag.Target;
        public bool IsMajor => Version.Minor == 0 && Version.Patch == 0;
        public bool IsMinor => Version.Patch == 0;


        public ReleaseInfo PreviousMajorRelease =>
            IsMajor
                ? Repository.FindRelease(new SemVersion(Version.Major - 1, 0, 0))
                : Repository.FindRelease(Version.Change(minor: 0, patch: 0));


        public ReleaseInfo PreviousMinorRelease =>
            Repository.ReleasesDescending
                .Where(r => r.Version < Version)
                .Where(r => r.IsMinor)
                .FirstOrDefault();


        public ReleaseInfo PreviousReleaseAncestor =>
            Commit.GetCommitsBackTo(null)
                .Skip(1)
                .Select(commit =>
                    Repository.GetReleases(commit)
                        .OrderBy(r => r.Version)
                        .LastOrDefault())
                .FirstOrDefault(r => r != null);


        public ReleaseInfo PreviousReleaseNumeric =>
            Repository.ReleasesDescending
                .Where(r => r.Version < Version)
                .FirstOrDefault();


        public IEnumerable<CommitInfo> CommitsSincePreviousReleaseAncestor =>
            Commit.GetCommitsSince(PreviousReleaseAncestor?.Commit);

    }
}
