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


        public ReleaseInfo(VerbotRepository repository, ReleaseTagInfo tagInfo)
        {
            Guard.NotNull(repository, nameof(repository));
            Guard.NotNull(tagInfo, nameof(tagInfo));

            Repository = repository;
            Version = tagInfo.Version;
            Tag = tagInfo.Ref;
        }


        public SemVersion Version { get; }
        public RefInfo Tag { get; }
        public CommitInfo Commit => Tag.Target;
        public bool IsMajor => Version.Minor == 0 && Version.Patch == 0;
        public bool IsMinor => Version.Minor > 0 && Version.Patch == 0;
        public bool IsPatch => Version.Patch > 0;


        public ReleaseInfo PreviousNumericRelease =>
            Repository.ReleasesDescending
                .Where(r => r.Version < Version)
                .FirstOrDefault();


        public ReleaseInfo PreviousAncestralRelease =>
            Commit.GetCommitsBackTo(null)
                .Skip(1)
                .Select(commit =>
                    Repository.GetReleases(commit)
                        .OrderBy(r => r.Version)
                        .LastOrDefault())
                .FirstOrDefault(r => r != null);


        public ReleaseInfo PreviousMajorRelease =>
            IsMajor
                ? Repository.FindRelease(new SemVersion(Version.Major - 1, 0, 0))
                : Repository.FindRelease(Version.Change(minor: 0, patch: 0));


        public ReleaseInfo PreviousNumericMajorOrMinorRelease =>
            Repository.ReleasesDescending
                .Where(r => r.Version < Version)
                .Where(r => r.IsMajor || r.IsMinor)
                .FirstOrDefault();


        public IEnumerable<CommitInfo> CommitsSincePreviousAncestralRelease =>
            Commit.GetCommitsSince(PreviousAncestralRelease?.Commit);

    }
}
