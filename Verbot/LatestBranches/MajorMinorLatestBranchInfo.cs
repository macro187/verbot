using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot.LatestBranches
{
    class MajorMinorLatestBranchInfo
    {

        public static bool IsMajorMinorLatestBranchName(string name)
        {
            Guard.NotNull(name, nameof(name));
            if (Regex.IsMatch(name, Pattern)) return true;
            return false;
        }


        const string Pattern = @"^(\d+)\.(\d+)-latest$";


        public MajorMinorLatestBranchInfo(GitRef branch)
        {
            Guard.NotNull(branch, nameof(branch));
            if (!branch.IsBranch) throw new ArgumentException("Not a branch", nameof(branch));

            var match = Regex.Match(branch.Name, Pattern);
            if (!match.Success) throw new ArgumentException("Not a MAJOR.MINOR-latest branch", nameof(branch));

            Branch = branch;
            Version = new SemVersion(
                int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
        }


        public GitRef Branch { get; }
        public SemVersion Version { get; }

    }
}
