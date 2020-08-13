using System;
using System.Collections.Generic;
using System.Linq;
using MacroCollections;
using MacroGit;
using MacroGuards;

namespace Verbot.Commits
{
    class CommitContext
    {

        readonly GitRepository GitRepository;


        public CommitContext(GitRepository gitRepository)
        {
            GitRepository = gitRepository;
        }


        IDictionary<GitSha1, CommitInfo> CommitCache =
            new Dictionary<GitSha1, CommitInfo>();


        public CommitInfo GetCommit(GitSha1 sha1) =>
            CommitCache.GetOrAdd(sha1, () =>
                RevList(1, sha1).Single());


        public IEnumerable<CommitInfo> GetCommitsForward(CommitInfo from, CommitInfo to) =>
            GetCommitsBackward(to, from).Reverse();


        public IEnumerable<CommitInfo> GetCommitsBackward(CommitInfo from, CommitInfo to)
        {
            Guard.NotNull(from, nameof(from));

            var commit = from;
            while (true)
            {
                if (commit == to)
                {
                    break;
                }

                if (commit == null)
                {
                    throw new InvalidOperationException($"from {from.Sha1} does not descend from to {to.Sha1}");
                }

                yield return commit;

                switch (commit.ParentSha1s.Count)
                {
                    case 1:
                        var parentSha1 = commit.ParentSha1s.Single();
                        if (!CommitCache.ContainsKey(parentSha1)) RevList(parentSha1);
                        commit = GetCommit(parentSha1);
                        break;
                    case 0:
                        commit = null;
                        break;
                    default:
                        throw new NotSupportedException($"Merge commit not supported {commit.Sha1}");
                }
            }
        }


        IEnumerable<CommitInfo> RevList(GitRev rev) =>
            RevList(-1, rev);


        IEnumerable<CommitInfo> RevList(int maxCount, GitRev rev) =>
            RevList(maxCount, new[] { rev });


        IEnumerable<CommitInfo> RevList(int maxCount, IEnumerable<GitRev> revs) =>
            GitRepository.RevList(maxCount, revs)
                .Select(gitCommit => GetOrAddCommit(gitCommit))
                .ToList();


        CommitInfo GetOrAddCommit(GitCommitInfo gitCommit) =>
            CommitCache.GetOrAdd(gitCommit.Sha1, () =>
                new CommitInfo(this, GitRepository, gitCommit));

    }
}
