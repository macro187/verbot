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
        string message;
        DateTimeOffset? committerDate;
        bool? isBreaking;
        bool? isFeature;

        
        /// <param name="sha1">
        /// Assumed to exist in <paramref name="gitRepository"/>
        /// </param>
        ///
        public CommitInfo(VerbotRepository verbotRepository, GitRepository gitRepository, GitSha1 sha1)
        {
            VerbotRepository = verbotRepository;
            GitRepository = gitRepository;
            Sha1 = sha1;
        }
        

        public GitSha1 Sha1 { get; }


        public string Message =>
            message ?? (message =
                GitRepository.GetCommitMessage(Sha1));
        

        public DateTimeOffset CommitterDate =>
            committerDate ?? (committerDate =
                GitRepository.GetCommitterDate(Sha1)).Value;


        public bool IsBreaking =>
            isBreaking ?? (isBreaking =
                StringExtensions.SplitLines(Message)
                    .Select(line => line.Trim())
                    .Any(line => Regex.IsMatch(line, @"^\+semver:\s?(breaking|major)$"))).Value;


        public bool IsFeature =>
            isFeature ?? (isFeature =
                StringExtensions.SplitLines(Message)
                    .Select(line => line.Trim())
                    .Any(line => Regex.IsMatch(line, @"^\+semver:\s?(feature|minor)$"))).Value;


        public GitShortSha1 GetShortSha1(int minimumLength) =>
            GitRepository.GetShortCommitId(Sha1, minimumLength);


        public bool DescendsFrom(CommitInfo commit) =>
            GitRepository.IsAncestor(commit.Sha1, Sha1);


        public IEnumerable<CommitInfo> CommitsSince(CommitInfo commit) =>
            VerbotRepository.GetCommitsBetween(commit?.Sha1, Sha1);

    }
}
