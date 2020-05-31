using System.Linq;
using MacroGit;
using System.Collections.Generic;
using System;

namespace Verbot
{
    partial class VerbotRepository
    {

        IDictionary<GitSha1, CommitInfo> CommitCache =
            new Dictionary<GitSha1, CommitInfo>();


        IDictionary<(GitSha1 from, GitSha1 to), IList<CommitInfo>> CommitsBetweenCache =
            new Dictionary<(GitSha1 from, GitSha1 to), IList<CommitInfo>>();


        CommitInfo GetHeadCommit() =>
            FindCommit(new GitRev("HEAD"))
                ?? throw new InvalidOperationException("No HEAD commit");


        CommitInfo FindCommit(GitRev rev) =>
            GitRepository.TryGetCommitId(rev, out var sha1) ? GetCommit(sha1) : null;


        public CommitInfo GetCommit(GitSha1 sha1) =>
            CommitCache.ContainsKey(sha1)
                ? CommitCache[sha1]
                : CommitCache[sha1] = new CommitInfo(this, GitRepository, sha1);


        public IEnumerable<CommitInfo> GetCommits(IEnumerable<GitSha1> sha1s) =>
            sha1s.Select(s => GetCommit(s));


        public IList<CommitInfo> GetCommitsBetween(GitSha1 from, GitSha1 to) =>
            CommitsBetweenCache.ContainsKey((from, to))
                ? CommitsBetweenCache[(from, to)]
                : CommitsBetweenCache[(from, to)] = GetCommits(GitRepository.ListCommits(from, to)).ToList();
            
    }
}
