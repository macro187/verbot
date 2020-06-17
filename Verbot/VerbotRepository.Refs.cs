using System.Collections.Generic;
using System.Linq;
using MacroGit;
using MacroSemver;

namespace Verbot
{
    partial class VerbotRepository
    {
        
        IEnumerable<RefInfo> RefsCache;
        IEnumerable<RefInfo> TagsCache;
        IEnumerable<RefInfo> BranchesCache;
        IEnumerable<ReleaseTagInfo> ReleaseTagCache;
        ILookup<CommitInfo, ReleaseTagInfo> CommitReleaseTagLookupCache;


        public IEnumerable<RefInfo> Refs =>
            RefsCache ?? (RefsCache =
                GitRepository.GetRefs()
                    .Select(r => new RefInfo(this, r))
                    .ToList());


        public RefInfo Head =>
            Refs.SingleOrDefault(r => r.FullName == "HEAD");


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


        IEnumerable<ReleaseTagInfo> GetReleaseTags(CommitInfo commit) =>
            CommitReleaseTagLookup.Contains(commit)
                ? CommitReleaseTagLookup[commit]
                : Enumerable.Empty<ReleaseTagInfo>();


        public RefInfo FindBranch(GitRefNameComponent name) =>
            Branches.Where(b => b.Name == name).SingleOrDefault();


        IEnumerable<GitRefWithRemote> GetRemoteInfo(IEnumerable<RefInfo> refs)
        {
            var remoteRefs = GitRepository.GetRemoteRefs().ToDictionary(r => r.FullName, r => r.Target);

            GitSha1 LookupRemoteTarget(GitFullRefName fullName) =>
                remoteRefs.TryGetValue(fullName, out var target) ? target : null;

            return refs.Select(r => new GitRefWithRemote(r, LookupRemoteTarget(r.FullName)));
        }

    }
}
