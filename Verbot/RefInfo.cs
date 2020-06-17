using MacroGit;
using MacroGuards;

namespace Verbot
{
    class RefInfo
    {

        readonly VerbotRepository verbotRepository;
        CommitInfo target;


        public RefInfo(VerbotRepository verbotRepository, GitRef @ref)
        {
            Guard.NotNull(verbotRepository, nameof(verbotRepository));
            Guard.NotNull(@ref, nameof(@ref));

            this.verbotRepository = verbotRepository;
            Name = @ref.Name;
            FullName = @ref.FullName;
            IsBranch = @ref.IsBranch;
            IsTag = @ref.IsTag;
            TargetSha1 = @ref.Target;
        }
        

        public GitRefNameComponent Name { get; }
        public GitFullRefName FullName { get; }
        public bool IsBranch { get; }
        public bool IsTag { get; }
        public GitSha1 TargetSha1 { get; }


        public CommitInfo Target =>
            target ?? (target = verbotRepository.GetCommit(TargetSha1));

    }
}
