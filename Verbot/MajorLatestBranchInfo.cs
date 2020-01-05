using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    internal class MajorLatestBranchInfo
    {

        public static bool IsMajorLatestBranchName(string name)
        {
            Guard.NotNull(name, nameof(name));
            if (Regex.IsMatch(name, Pattern)) return true;
            return false;
        }


        const string Pattern = @"^(\d+)-latest$";


        public MajorLatestBranchInfo(VerbotRepository repository, GitCommitName name)
        {
            Guard.NotNull(repository, nameof(repository));
            var match = Regex.Match(name, Pattern);
            if (!match.Success) throw new ArgumentException("Not a MAJOR-latest branch name", "name");
            Repository = repository;
            Name = name;
            Version = new SemVersion(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
        }


        public VerbotRepository Repository { get; }
        public GitCommitName Name { get; }
        public SemVersion Version { get; }

    }
}
