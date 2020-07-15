using MacroGit;
using MacroGuards;

namespace Verbot
{
    class RefInfo
    {

        readonly RefContext RefContext;
        readonly CommitContext CommitContext;
        readonly GitRef Ref;


        public RefInfo(RefContext refContext, CommitContext commitContext, GitRef @ref)
        {
            Guard.NotNull(refContext, nameof(refContext));
            Guard.NotNull(commitContext, nameof(commitContext));
            Guard.NotNull(@ref, nameof(@ref));

            RefContext = refContext;
            CommitContext = commitContext;
            Ref = @ref;
        }
        

        public GitRefNameComponent Name => Ref.Name;
        public GitFullRefName FullName => Ref.FullName;
        public bool IsBranch => Ref.IsBranch;
        public bool IsTag => Ref.IsTag;
        public GitSha1 TargetSha1 => Ref.Target;
        public CommitInfo Target => CommitContext.GetCommit(TargetSha1);
        public RefInfo SymbolicTarget => RefContext.FindSymbolicRefTarget(this);

    }
}
