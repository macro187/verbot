using MacroGit;
using MacroGuards;
using Verbot.Commits;

namespace Verbot.Refs
{
    class BranchInfo : RefInfo
    {

        protected BranchInfo(RefContext refContext, CommitContext commitContext, GitRef @ref)
            : base(refContext, commitContext, @ref)
        {
        }


        public static BranchInfo TryCreate(RefContext refContext, CommitContext commitContext, GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsBranch) return null;
            return new BranchInfo(refContext, commitContext, @ref);
        }

    }
}
