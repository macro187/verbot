using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using MacroSemver;
using Verbot.Commits;

namespace Verbot.Refs
{
    class MajorLatestBranchInfo : LatestBranchInfo
    {

        protected MajorLatestBranchInfo(
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


        public new static MajorLatestBranchInfo TryCreate(
            RefContext refContext,
            CommitContext commitContext,
            GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (!@ref.IsBranch) return null;
            var match = Regex.Match(@ref.Name, @"^(\d+)-latest$");
            if (!match.Success) return null;
            var major = int.Parse(match.Groups[1].Value);
            var series = new SemVersion(major);
            return new MajorLatestBranchInfo(refContext, commitContext, @ref, series);
        }

    }
}
