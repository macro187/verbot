using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;

namespace Verbot
{
    class CommitInfo
    {

        readonly VerbotRepository VerbotRepository;
        readonly GitRepository GitRepository;
        readonly GitCommitInfo GitCommit;
        

        public CommitInfo(VerbotRepository verbotRepository, GitRepository gitRepository, GitCommitInfo gitCommit)
        {
            Guard.NotNull(verbotRepository, nameof(verbotRepository));
            Guard.NotNull(gitRepository, nameof(gitRepository));
            Guard.NotNull(gitCommit, nameof(gitCommit));
            VerbotRepository = verbotRepository;
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


        public bool DescendsFrom(CommitInfo commit) =>
            GitRepository.IsAncestor(commit.Sha1, Sha1);


        public IEnumerable<CommitInfo> CommitsSince(CommitInfo commit) =>
            VerbotRepository.GetCommitsForward(commit, this);

    }
}
