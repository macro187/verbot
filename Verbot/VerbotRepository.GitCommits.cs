using System.Linq;
using MacroGit;
using System.Collections.Generic;
using System;

namespace Verbot
{
    partial class VerbotRepository
    {

        IDictionary<GitSha1, VerbotCommitInfo> CommitCache =
            new Dictionary<GitSha1, VerbotCommitInfo>();


        IDictionary<(GitSha1 from, GitSha1 to), IList<VerbotCommitInfo>> CommitsBetweenCache =
            new Dictionary<(GitSha1 from, GitSha1 to), IList<VerbotCommitInfo>>();


        VerbotCommitInfo GetHeadCommit() =>
            FindCommit(new GitRev("HEAD"))
                ?? throw new InvalidOperationException("No HEAD commit");


        VerbotCommitInfo FindCommit(GitRev rev) =>
            GitRepository.TryGetCommitId(rev, out var sha1) ? GetCommit(sha1) : null;


        public VerbotCommitInfo GetCommit(GitSha1 sha1) =>
            CommitCache.ContainsKey(sha1)
                ? CommitCache[sha1]
                : CommitCache[sha1] = new VerbotCommitInfo(this, GitRepository, sha1);


        public IEnumerable<VerbotCommitInfo> GetCommits(IEnumerable<GitSha1> sha1s) =>
            sha1s.Select(s => GetCommit(s));


        public IList<VerbotCommitInfo> GetCommitsBetween(GitSha1 from, GitSha1 to) =>
            CommitsBetweenCache.ContainsKey((from, to))
                ? CommitsBetweenCache[(from, to)]
                : CommitsBetweenCache[(from, to)] = GetCommits(GitRepository.ListCommits(from, to)).ToList();
            
    }
}
