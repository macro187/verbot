using MacroGit;
using Verbot.Commits;

namespace Verbot.Refs
{
    abstract class MasterBranchInfo : BranchInfo
    {

        protected MasterBranchInfo(RefContext refContext, CommitContext commitContext, GitRef @ref)
            : base(refContext, commitContext, @ref)
        {
        }

    }
}
