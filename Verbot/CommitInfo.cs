using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MacroGit;
using MacroSystem;

namespace Verbot
{
    class CommitInfo
    {

        readonly VerbotRepository VerbotRepository;
        readonly GitRepository GitRepository;

        
        /// <param name="sha1">
        /// Assumed to exist in <paramref name="gitRepository"/>
        /// </param>
        ///
        public CommitInfo(VerbotRepository verbotRepository, GitRepository gitRepository, GitCommitInfo gitCommit)
        {
            VerbotRepository = verbotRepository;
            GitRepository = gitRepository;
            Sha1 = gitCommit.Sha1;
            Author = gitCommit.Author;
            AuthorDate = gitCommit.AuthorDate;
            Committer = gitCommit.Committer;
            CommitDate = gitCommit.CommitDate;
            MessageLines = gitCommit.MessageLines;
            Message = gitCommit.Message;
            IsBreaking = MessageLines.Any(line => Regex.IsMatch(line.Trim(), @"^\+semver:\s?(breaking|major)$"));
            IsFeature = MessageLines.Any(line => Regex.IsMatch(line.Trim(), @"^\+semver:\s?(feature|minor)$"));
        }
        

        public GitSha1 Sha1 { get; }
        public string Author { get; }
        public DateTimeOffset AuthorDate { get; }
        public string Committer { get; }
        public DateTimeOffset CommitDate { get; }
        public IReadOnlyList<string> MessageLines { get; }
        public string Message { get; }
        public bool IsBreaking { get; }
        public bool IsFeature { get; }


        public bool DescendsFrom(CommitInfo commit) =>
            GitRepository.IsAncestor(commit.Sha1, Sha1);


        public IEnumerable<CommitInfo> CommitsSince(CommitInfo commit) =>
            VerbotRepository.GetCommitsBetween(commit?.Sha1, Sha1);

    }
}
