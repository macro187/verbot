using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using MacroSemver;
using Verbot.Commits;

namespace Verbot.Refs
{
    class MinorMasterBranchInfo : MasterBranchInfo
    {

        protected MinorMasterBranchInfo(
            RefContext refContext,
            CommitContext commitContext,
            GitRef @ref,
            SemVersion series
        )
            : base(refContext, commitContext, @ref)
        {
            Series = series;
        }


        public SemVersion Series { get; }


        public new static MinorMasterBranchInfo TryCreate(
            RefContext refContext,
            CommitContext commitContext,
            GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsBranch) return null;
            var match = Regex.Match(@ref.Name, @"^(\d+)\.(\d+)-master$");
            if (!match.Success) return null;
            var major = int.Parse(match.Groups[1].Value);
            var minor = int.Parse(match.Groups[2].Value);
            var series = new SemVersion(major, minor);
            return new MinorMasterBranchInfo(refContext, commitContext, @ref, series);
        }

    }
}
