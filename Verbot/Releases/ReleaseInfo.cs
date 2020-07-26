using System.Collections.Generic;
using System.Linq;
using MacroGuards;
using MacroSemver;
using Verbot.Commits;
using Verbot.Refs;

namespace Verbot.Releases
{
    class ReleaseInfo
    {

        readonly ReleaseContext ReleaseContext;


        public ReleaseInfo(ReleaseContext releaseContext, ReleaseTagInfo tag)
        {
            Guard.NotNull(releaseContext, nameof(releaseContext));
            Guard.NotNull(tag, nameof(tag));

            ReleaseContext = releaseContext;
            Version = tag.Version;
            Tag = tag;
        }


        public SemVersion Version { get; }
        public ReleaseTagInfo Tag { get; }
        public CommitInfo Commit => Tag.Target;
        public bool IsMajor => Version.Minor == 0 && Version.Patch == 0;
        public bool IsMinor => Version.Minor > 0 && Version.Patch == 0;
        public bool IsPatch => Version.Patch > 0;


        public ReleaseInfo PreviousNumericRelease =>
            ReleaseContext.ReleasesDescending
                .Where(r => r.Version < Version)
                .FirstOrDefault();


        public ReleaseInfo PreviousAncestralRelease =>
            Commit.GetCommitsBackTo(null)
                .Skip(1)
                .Select(commit =>
                    ReleaseContext.GetReleases(commit)
                        .OrderBy(r => r.Version)
                        .LastOrDefault())
                .FirstOrDefault(r => r != null);


        public ReleaseInfo PreviousMajorRelease =>
            IsMajor
                ? ReleaseContext.FindRelease(new SemVersion(Version.Major - 1, 0, 0))
                : ReleaseContext.FindRelease(Version.Change(minor: 0, patch: 0));


        public ReleaseInfo PreviousNumericMajorOrMinorRelease =>
            ReleaseContext.ReleasesDescending
                .Where(r => r.Version < Version)
                .Where(r => r.IsMajor || r.IsMinor)
                .FirstOrDefault();


        public IEnumerable<CommitInfo> CommitsSincePreviousAncestralRelease =>
            Commit.GetCommitsSince(PreviousAncestralRelease?.Commit);

    }
}
