using System.Collections.Generic;
using System.Linq;
using MacroCollections;
using MacroGit;
using Verbot.Commits;

namespace Verbot.Refs
{
    class RefContext
    {

        readonly GitRepository GitRepository;
        readonly CommitContext CommitContext;


        public RefContext(GitRepository gitRepository, CommitContext commitContext)
        {
            GitRepository = gitRepository;
            CommitContext = commitContext;
        }


        IEnumerable<RefInfo> RefsCache;
        ILookup<CommitInfo, ReleaseTagInfo> CommitReleaseTagLookupCache;
        IDictionary<RefInfo, BranchInfo> SymbolicRefTargetsCache = new Dictionary<RefInfo, BranchInfo>();


        public IEnumerable<RefInfo> Refs =>
            RefsCache ??=
                GitRepository.GetRefs()
                    .Select(r => RefInfo.Create(this, CommitContext, r))
                    .ToList();


        public RefInfo Head =>
            Refs.SingleOrDefault(r => r.FullName == "HEAD");


        public BranchInfo FindSymbolicRefTarget(RefInfo @ref) =>
            SymbolicRefTargetsCache.GetOrAdd(@ref, () =>
            {
                var fullName = GitRepository.FindSymbolicRefTarget(@ref.FullName);
                if (fullName == null) return null;
                return
                    Refs
                        .Where(r => r.FullName == fullName)
                        .Cast<BranchInfo>()
                        .SingleOrDefault();
            });


        public IEnumerable<TagInfo> Tags =>
            Refs.OfType<TagInfo>();


        public IEnumerable<BranchInfo> Branches =>
            Refs.OfType<BranchInfo>();


        public IEnumerable<ReleaseTagInfo> ReleaseTags =>
            Refs.OfType<ReleaseTagInfo>();


        ILookup<CommitInfo, ReleaseTagInfo> CommitReleaseTagLookup =>
            CommitReleaseTagLookupCache ??=
                ReleaseTags.ToLookup(tag => tag.Target);


        public IEnumerable<ReleaseTagInfo> GetReleaseTags(CommitInfo commit) =>
            CommitReleaseTagLookup.Contains(commit)
                ? CommitReleaseTagLookup[commit]
                : Enumerable.Empty<ReleaseTagInfo>();


        public RefInfo FindBranch(GitRefNameComponent name) =>
            Branches.Where(b => b.Name == name).SingleOrDefault();


        public IEnumerable<MasterBranchInfo> MasterBranches =>
            Refs.OfType<MasterBranchInfo>();


        public IEnumerable<GitRefWithRemote> GetRemoteInfo(IEnumerable<RefInfo> refs)
        {
            var remoteRefs = GitRepository.GetRemoteRefs().ToDictionary(r => r.FullName, r => r.Target);

            GitSha1 LookupRemoteTarget(GitFullRefName fullName) =>
                remoteRefs.TryGetValue(fullName, out var target) ? target : null;

            return refs.Select(r => new GitRefWithRemote(r, LookupRemoteTarget(r.FullName)));
        }

    }
}
