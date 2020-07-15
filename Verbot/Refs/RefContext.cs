using System.Collections.Generic;
using System.Linq;
using MacroGit;
using MacroSemver;
using MacroCollections;
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
        IEnumerable<RefInfo> TagsCache;
        IEnumerable<RefInfo> BranchesCache;
        IEnumerable<ReleaseTagInfo> ReleaseTagCache;
        ILookup<CommitInfo, ReleaseTagInfo> CommitReleaseTagLookupCache;
        IDictionary<RefInfo, RefInfo> SymbolicRefTargetsCache = new Dictionary<RefInfo, RefInfo>();


        public IEnumerable<RefInfo> Refs =>
            RefsCache ?? (RefsCache =
                GitRepository.GetRefs()
                    .Select(r => new RefInfo(this, CommitContext, r))
                    .ToList());


        public RefInfo Head =>
            Refs.SingleOrDefault(r => r.FullName == "HEAD");


        public RefInfo FindSymbolicRefTarget(RefInfo @ref) =>
            SymbolicRefTargetsCache.GetOrAdd(@ref, () =>
            {
                var name = GitRepository.FindSymbolicRefTarget(@ref.FullName);
                if (name == null) return null;
                return Refs.Where(r => r.FullName == name).SingleOrDefault();
            });


        public IEnumerable<RefInfo> Tags =>
            TagsCache ?? (TagsCache =
                Refs.Where(r => r.IsTag).ToList());


        public IEnumerable<RefInfo> Branches =>
            BranchesCache ?? (BranchesCache =
                Refs.Where(r => r.IsBranch).ToList());


        public IEnumerable<ReleaseTagInfo> ReleaseTags =>
            ReleaseTagCache ?? (ReleaseTagCache =
                Tags
                    .Select(tag =>
                    {
                        SemVersion.TryParse(tag.Name, out var version, true);
                        return (Ref: tag, Version: version);
                    })
                    .Where(tag => tag.Version != null)
                    .Where(tag => tag.Version.Prerelease == "")
                    .Where(tag => tag.Version.Build == "")
                    .Select(tag => new ReleaseTagInfo(tag.Version, tag.Ref))
                    .ToList());


        ILookup<CommitInfo, ReleaseTagInfo> CommitReleaseTagLookup =>
            CommitReleaseTagLookupCache ?? (CommitReleaseTagLookupCache =
                ReleaseTags.ToLookup(tag => tag.Ref.Target));


        public IEnumerable<ReleaseTagInfo> GetReleaseTags(CommitInfo commit) =>
            CommitReleaseTagLookup.Contains(commit)
                ? CommitReleaseTagLookup[commit]
                : Enumerable.Empty<ReleaseTagInfo>();


        public RefInfo FindBranch(GitRefNameComponent name) =>
            Branches.Where(b => b.Name == name).SingleOrDefault();


        // IEnumerable<RefInfo> MasterBranches =>
        //     Branches
        //         .Where(branch => IsMasterBranchName(branch.Name));


        public IEnumerable<GitRefWithRemote> GetRemoteInfo(IEnumerable<RefInfo> refs)
        {
            var remoteRefs = GitRepository.GetRemoteRefs().ToDictionary(r => r.FullName, r => r.Target);

            GitSha1 LookupRemoteTarget(GitFullRefName fullName) =>
                remoteRefs.TryGetValue(fullName, out var target) ? target : null;

            return refs.Select(r => new GitRefWithRemote(r, LookupRemoteTarget(r.FullName)));
        }


        // static bool IsMasterBranchName(string name)
        // {
        //     if (name == "master") return true;
        //     if (Regex.IsMatch(name, @"^(\d+)\.(\d+)-master$")) return true;
        //     return false;
        // }

    }
}
