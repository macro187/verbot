using System;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    class ReleaseTagInfo
    {

        public ReleaseTagInfo(GitRef @ref, SemVersion version, VerbotCommitInfo target)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsTag) throw new ArgumentException("Not a tag", nameof(@ref));
            Guard.NotNull(version, nameof(version));
            Guard.NotNull(target, nameof(target));

            Ref = @ref;
            Version = version;
            Target = target;
        }


        public GitRef Ref { get; }
        public SemVersion Version { get; }
        public VerbotCommitInfo Target { get; }
        public GitRefNameComponent Name => Ref.Name;
        public GitFullRefName FullName => Ref.FullName;

    }
}
