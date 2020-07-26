using MacroGit;
using MacroGuards;
using MacroSemver;
using Verbot.Commits;

namespace Verbot.Refs
{
    class ReleaseTagInfo : TagInfo
    {

        protected ReleaseTagInfo(RefContext refContext, CommitContext commitContext, GitRef @ref, SemVersion version)
            : base(refContext, commitContext, @ref)
        {
            Version = version;
        }


        public SemVersion Version { get; }


        public new static ReleaseTagInfo TryCreate(RefContext refContext, CommitContext commitContext, GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsTag) return null;
            if (!SemVersion.TryParse(@ref.Name, out var version)) return null;
            if (version.Prerelease != "") return null;
            if (version.Build != "") return null;
            return new ReleaseTagInfo(refContext, commitContext, @ref, version);
        }

    }
}
