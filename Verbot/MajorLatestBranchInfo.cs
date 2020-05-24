using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    class MajorLatestBranchInfo
    {

        public static bool IsMajorLatestBranchName(string name)
        {
            Guard.NotNull(name, nameof(name));
            if (Regex.IsMatch(name, Pattern)) return true;
            return false;
        }


        const string Pattern = @"^(\d+)-latest$";


        public MajorLatestBranchInfo(GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            var match = Regex.Match(@ref.Name, Pattern);
            if (!match.Success) throw new ArgumentException("Not a MAJOR-latest branch", nameof(@ref));
            Ref = @ref;
            Version = new SemVersion(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
        }


        public GitRef Ref { get; }
        public GitRefNameComponent Name => Ref.Name;
        public SemVersion Version { get; }

    }
}
