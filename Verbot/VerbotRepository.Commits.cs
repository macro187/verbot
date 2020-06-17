using System.Linq;
using MacroGit;
using System.Collections.Generic;
using System.Diagnostics;

namespace Verbot
{
    partial class VerbotRepository
    {

        IDictionary<GitSha1, CommitInfo> CommitCache =
            new Dictionary<GitSha1, CommitInfo>();


        IDictionary<(GitSha1 from, GitSha1 to), IReadOnlyList<CommitInfo>> CommitsBetweenCache =
            new Dictionary<(GitSha1 from, GitSha1 to), IReadOnlyList<CommitInfo>>();


        public CommitInfo GetCommit(GitSha1 sha1) =>
            CommitCache.TryGetValue(sha1, out var commits)
                ? commits
                : CommitCache[sha1] = GetCommits(sha1, 1).Single();


        //
        // TODO
        // Cache all subpaths too then preload tags and branches in descending alphabetical order to minimize the
        // number of Git invocations required.
        //
        public IReadOnlyList<CommitInfo> GetCommitsBetween(GitSha1 from, GitSha1 to) =>
            from == to
                ? new List<CommitInfo>()
                : CommitsBetweenCache.TryGetValue((from, to), out var commits)
                    ? commits
                    : CommitsBetweenCache[(from, to)] = GetCommits(RangeRev(from, to), -1).ToList();


        IEnumerable<CommitInfo> GetCommits(GitRev rev, int maxCount) =>
            GitRepository.RevList(rev, maxCount)
                .Select(gitCommit => GetCommit(gitCommit));


        CommitInfo GetCommit(GitCommitInfo gitCommit) =>
            CommitCache.TryGetValue(gitCommit.Sha1, out var commit)
                ? commit
                : CommitCache[gitCommit.Sha1] = new CommitInfo(this, GitRepository, gitCommit);


        static GitRev RangeRev(GitRev from, GitRev to) =>
            new GitRev(
                string.Concat(
                    from ?? "",
                    from != null ? ".." : "",
                    to));

    }
}
