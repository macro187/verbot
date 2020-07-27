using MacroGit;
using MacroGuards;
using Verbot.Commits;

namespace Verbot.Refs
{
    class TheLatestBranchInfo : LatestBranchInfo
    {

        protected TheLatestBranchInfo(RefContext refContext, CommitContext commitContext, GitRef @ref)
            : base(refContext, commitContext, @ref)
        {
        }


        public new static TheLatestBranchInfo TryCreate(RefContext refContext, CommitContext commitContext, GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsBranch) return null;
            if (@ref.Name != "latest") return null;
            return new TheLatestBranchInfo(refContext, commitContext, @ref);
        }

    }
}
