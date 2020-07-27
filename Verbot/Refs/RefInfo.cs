using MacroGit;
using MacroGuards;
using Verbot.Commits;

namespace Verbot.Refs
{
    class RefInfo
    {

        readonly RefContext RefContext;
        readonly CommitContext CommitContext;
        readonly GitRef Ref;


        protected RefInfo(RefContext refContext, CommitContext commitContext, GitRef @ref)
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
        public GitSha1 TargetSha1 => Ref.Target;
        public CommitInfo Target => CommitContext.GetCommit(TargetSha1);
        public RefInfo SymbolicTarget => RefContext.FindSymbolicRefTarget(this);


        public static RefInfo Create(RefContext refContext, CommitContext commitContext, GitRef @ref) =>
            MajorLatestBranchInfo.TryCreate(refContext, commitContext, @ref) ??
            MinorLatestBranchInfo.TryCreate(refContext, commitContext, @ref) ??
            TheLatestBranchInfo.TryCreate(refContext, commitContext, @ref) ??
            MinorMasterBranchInfo.TryCreate(refContext, commitContext, @ref) ??
            TheMasterBranchInfo.TryCreate(refContext, commitContext, @ref) ??
            ReleaseTagInfo.TryCreate(refContext, commitContext, @ref) ??
            TagInfo.TryCreate(refContext, commitContext, @ref) ??
            BranchInfo.TryCreate(refContext, commitContext, @ref) ??
            new RefInfo(refContext, commitContext, @ref);

    }
}
