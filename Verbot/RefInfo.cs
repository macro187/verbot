using MacroGit;
using MacroGuards;

namespace Verbot
{
    class RefInfo
    {

        readonly VerbotRepository VerbotRepository;
        readonly GitRef Ref;


        public RefInfo(VerbotRepository verbotRepository, GitRef @ref)
        {
            Guard.NotNull(verbotRepository, nameof(verbotRepository));
            Guard.NotNull(@ref, nameof(@ref));

            VerbotRepository = verbotRepository;
            Ref = @ref;
        }
        

        public GitRefNameComponent Name => Ref.Name;
        public GitFullRefName FullName => Ref.FullName;
        public bool IsBranch => Ref.IsBranch;
        public bool IsTag => Ref.IsTag;
        public GitSha1 TargetSha1 => Ref.Target;
        public CommitInfo Target => VerbotRepository.GetCommit(TargetSha1);

    }
}
