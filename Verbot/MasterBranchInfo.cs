using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    class MasterBranchInfo
    {

        public MasterBranchInfo(GitRef @ref, SemVersion version)
        {
            Guard.NotNull(@ref, nameof(@ref));
            Guard.NotNull(version, nameof(version));
            Ref = @ref;
            Version = version;
        }


        public GitRef Ref { get; }
        public GitRefNameComponent Name => Ref.Name;
        public SemVersion Version { get; }

    }
}
