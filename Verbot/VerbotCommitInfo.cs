using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MacroGit;
using MacroSystem;

namespace Verbot
{
    partial class VerbotCommitInfo
    {

        readonly VerbotRepository VerbotRepository;
        readonly GitRepository GitRepository;
        string message;
        bool? isBreaking;
        bool? isFeature;

        
        /// <param name="sha1">
        /// Assumed to exist in <paramref name="gitRepository"/>
        /// </param>
        ///
        public VerbotCommitInfo(VerbotRepository verbotRepository, GitRepository gitRepository, GitSha1 sha1)
        {
            VerbotRepository = verbotRepository;
            GitRepository = gitRepository;
            Sha1 = sha1;
        }
        

        public GitSha1 Sha1 { get; }


        public string Message
        {
            get
            {
                if (message == null)
                {
                    message = GitRepository.GetCommitMessage(Sha1);
                }

                return message;
            }
        }


        public bool IsBreaking
        {
            get
            {
                if (isBreaking == null)
                {
                    var lines = StringExtensions.SplitLines(Message).Select(line => line.Trim());
                    isBreaking = lines.Any(line => Regex.IsMatch(line, @"^\+semver:\s?(breaking|major)$"));
                }

                return isBreaking.Value;
            }
        }


        public bool IsFeature
        {
            get
            {
                if (isFeature == null)
                {
                    var lines = StringExtensions.SplitLines(Message).Select(line => line.Trim());
                    isFeature = lines.Any(line => Regex.IsMatch(line, @"^\+semver:\s?(feature|minor)$"));
                }

                return isFeature.Value;
            }
        }


        public bool DescendsFrom(VerbotCommitInfo commit) =>
            GitRepository.IsAncestor(commit.Sha1, Sha1);


        public IEnumerable<VerbotCommitInfo> ListCommitsFrom(VerbotCommitInfo commit) =>
            VerbotRepository.GetCommits(GitRepository.ListCommits(commit?.Sha1, Sha1));

    }
}
