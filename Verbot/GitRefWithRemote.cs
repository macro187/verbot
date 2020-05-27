using MacroGit;

namespace Verbot
{
    class GitRefWithRemote
    {

        public GitRefWithRemote(GitRef @ref, GitSha1 remoteTarget)
        {
            Ref = @ref;
            RemoteTarget = remoteTarget;
        }


        public GitRef Ref { get; }
        public GitRefNameComponent Name => Ref.Name;
        public GitFullRefName FullName => Ref.FullName;
        public GitSha1 Target => Ref.Target;
        public bool IsBranch => Ref.IsBranch;
        public bool IsTag => Ref.IsTag;
        public GitSha1 RemoteTarget { get; }

    }
}
