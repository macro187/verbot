using MacroGit;
using MacroGuards;

namespace Verbot
{
    class RefInfo
    {

        public RefInfo(GitRef @ref, CommitInfo target)
        {
            Guard.NotNull(@ref, nameof(@ref));
            Guard.NotNull(target, nameof(target));

            Name = @ref.Name;
            FullName = @ref.FullName;
            Target = target;
            IsBranch = @ref.IsBranch;
            IsTag = @ref.IsTag;
        }
        

        public GitRefNameComponent Name { get; }
        public GitFullRefName FullName { get; }
        public CommitInfo Target { get; }
        public bool IsBranch { get; }
        public bool IsTag { get; }

    }
}
