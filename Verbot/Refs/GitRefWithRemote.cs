using MacroGit;
using Verbot.Commits;

namespace Verbot.Refs
{
    class GitRefWithRemote
    {

        public GitRefWithRemote(RefInfo @ref, GitSha1 remoteTargetSha1)
        {
            Ref = @ref;
            RemoteTargetSha1 = remoteTargetSha1;
        }


        public RefInfo Ref { get; }
        public GitRefNameComponent Name => Ref.Name;
        public GitFullRefName FullName => Ref.FullName;
        public CommitInfo Target => Ref.Target;
        public bool IsBranch => Ref.IsBranch;
        public bool IsTag => Ref.IsTag;
        public GitSha1 RemoteTargetSha1 { get; }

    }
}
