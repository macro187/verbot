using System;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    class ReleaseInfo
    {

        public ReleaseInfo(SemVersion version, VerbotCommitInfo commit, GitRef tag)
        {
            Guard.NotNull(tag, nameof(tag));
            if (!tag.IsTag) throw new ArgumentException("Not a tag", nameof(tag));
            Guard.NotNull(version, nameof(version));
            Guard.NotNull(commit, nameof(commit));

            Version = version;
            Commit = commit;
            Tag = tag;
        }


        public SemVersion Version { get; }
        public VerbotCommitInfo Commit { get; }
        public GitRef Tag { get; }

    }
}
