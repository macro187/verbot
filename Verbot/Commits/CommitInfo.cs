using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;

namespace Verbot.Commits
{
    class CommitInfo
    {

        readonly CommitContext CommitContext;
        readonly GitRepository GitRepository;
        readonly GitCommitInfo GitCommit;


        public CommitInfo(CommitContext commitContext, GitRepository gitRepository, GitCommitInfo gitCommit)
        {
            Guard.NotNull(commitContext, nameof(commitContext));
            Guard.NotNull(gitRepository, nameof(gitRepository));
            Guard.NotNull(gitCommit, nameof(gitCommit));
            CommitContext = commitContext;
            GitRepository = gitRepository;
            GitCommit = gitCommit;
            IsBreaking = MessageLines.Any(line => Regex.IsMatch(line.Trim(), @"^\+semver:\s?(breaking|major)$"));
            IsFeature = MessageLines.Any(line => Regex.IsMatch(line.Trim(), @"^\+semver:\s?(feature|minor)$"));
        }


        public bool IsBreaking { get; }
        public bool IsFeature { get; }
        public GitSha1 Sha1 => GitCommit.Sha1;
        public IReadOnlyList<GitSha1> ParentSha1s => GitCommit.ParentSha1s;
        public string Author => GitCommit.Author;
        public DateTimeOffset AuthorDate => GitCommit.AuthorDate;
        public string Committer => GitCommit.Committer;
        public DateTimeOffset CommitDate => GitCommit.CommitDate;
        public IReadOnlyList<string> MessageLines => GitCommit.MessageLines;
        public string Message => GitCommit.Message;


        public bool IsDescendentOf(CommitInfo commit) =>
            GitRepository.IsAncestor(commit.Sha1, Sha1);


        public IEnumerable<CommitInfo> GetCommitsSinceBeginning() =>
            GetCommitsSince(null);


        public IEnumerable<CommitInfo> GetCommitsBackToBeginning() =>
            GetCommitsBackTo(null);


        public IEnumerable<CommitInfo> GetCommitsSince(CommitInfo from) =>
            CommitContext.GetCommitsForward(from, this);


        public IEnumerable<CommitInfo> GetCommitsBackTo(CommitInfo to) =>
            CommitContext.GetCommitsBackward(this, to);

    }
}
