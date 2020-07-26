using MacroGit;
using MacroGuards;
using Verbot.Commits;

namespace Verbot.Refs
{
    class TagInfo : RefInfo
    {

        protected TagInfo(RefContext refContext, CommitContext commitContext, GitRef @ref)
            : base(refContext, commitContext, @ref)
        {
        }


        public static TagInfo TryCreate(RefContext refContext, CommitContext commitContext, GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsTag) return null;
            return new TagInfo(refContext, commitContext, @ref);
        }

    }
}
