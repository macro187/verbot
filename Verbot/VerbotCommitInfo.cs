using System;
using System.Linq;
using System.Text.RegularExpressions;
using MacroGit;
using MacroSystem;

namespace Verbot
{
    partial class VerbotCommitInfo
    {

        readonly GitRepository GitRepository;
        string message;
        bool? isBreaking;
        bool? isFeature;

        
        /// <param name="sha1">
        /// Assumed to exist in <paramref name="gitRepository"/>
        /// </param>
        ///
        public VerbotCommitInfo(GitRepository gitRepository, GitSha1 sha1)
        {
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

    }
}
