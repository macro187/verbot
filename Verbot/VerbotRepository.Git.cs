using System.Collections.Generic;
using System.Linq;
using MacroGit;

namespace Verbot
{
    partial class VerbotRepository
    {
        
        IEnumerable<GitRef> RefsCache;


        public IEnumerable<GitRef> Refs =>
            RefsCache ?? (RefsCache =
                GitRepository.GetRefs().ToList());


        public IEnumerable<GitRef> Tags =>
            Refs.Where(r => r.IsTag);


        public IEnumerable<GitRef> Branches =>
            Refs.Where(r => r.IsBranch);


        public GitRef FindBranch(GitRefNameComponent name) =>
            Branches.Where(b => b.Name == name).SingleOrDefault();


        IEnumerable<GitRefWithRemote> GetRemoteInfo(IEnumerable<GitRef> refs)
        {
            var remoteRefs = GitRepository.GetRemoteRefs().ToDictionary(r => r.FullName, r => r.Target);

            GitSha1 LookupRemoteTarget(GitFullRefName fullName) =>
                remoteRefs.TryGetValue(fullName, out var target) ? target : null;

            return refs.Select(r => new GitRefWithRemote(r, LookupRemoteTarget(r.FullName)));
        }

    }
}
