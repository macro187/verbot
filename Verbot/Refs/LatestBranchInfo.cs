using MacroGit;
using Verbot.Commits;

namespace Verbot.Refs
{
    abstract class LatestBranchInfo : BranchInfo
    {

        protected LatestBranchInfo(RefContext refContext, CommitContext commitContext, GitRef @ref)
            : base(refContext, commitContext, @ref)
        {
        }

    }
}
