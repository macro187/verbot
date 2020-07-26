using MacroGit;
using MacroGuards;
using Verbot.Commits;

namespace Verbot.Refs
{
    class TheMasterBranchInfo : MasterBranchInfo
    {

        protected TheMasterBranchInfo(RefContext refContext, CommitContext commitContext, GitRef @ref)
            : base(refContext, commitContext, @ref)
        {
        }


        public new static TheMasterBranchInfo TryCreate(RefContext refContext, CommitContext commitContext, GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsBranch) return null;
            if (@ref.Name != "master") return null;
            return new TheMasterBranchInfo(refContext, commitContext, @ref);
        }

    }
}
