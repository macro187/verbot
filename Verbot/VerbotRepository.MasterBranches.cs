using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using MacroGit;
using MacroGuards;
using MacroSemver;

namespace Verbot
{
    partial class VerbotRepository
    {
        
        IEnumerable<MasterBranchInfo> MasterBranchesCache;


        /// <summary>
        /// All 'master' and 'MAJOR.MINOR-master' branches, in decreasing order of tracked minor
        /// version
        /// </summary>
        ///
        public IEnumerable<MasterBranchInfo> MasterBranches =>
            MasterBranchesCache ?? (MasterBranchesCache =
                Branches
                    .Select(b => (Ref: b, Version: GetMasterBranchVersion(b)))
                    .Where(b => b.Version != null)
                    .Select(b => new MasterBranchInfo(b.Ref, b.Version))
                    .OrderByDescending(b => b.Version)
                    .ToList());


        IEnumerable<GitRef> VerbotBranches =>
            Branches
                .Where(b =>
                    GetMasterBranchVersion(b) != null ||
                    b.Name == "latest" ||
                    MajorLatestBranchInfo.IsMajorLatestBranchName(b.Name) ||
                    MajorMinorLatestBranchInfo.IsMajorMinorLatestBranchName(b.Name))
                .ToList();


        public IEnumerable<GitRefWithRemote> GetVerbotBranchesWithRemote() =>
            GetRemoteInfo(VerbotBranches).ToList();


        /// <summary>
        /// Find information about all 'MAJOR-latest' branches, in decreasing version order
        /// </summary>
        ///
        IList<MajorLatestBranchInfo> FindMajorLatestBranches()
        {
            return
                GitRepository.GetBranches()
                    .Where(b => MajorLatestBranchInfo.IsMajorLatestBranchName(b.Name))
                    .Select(b => new MajorLatestBranchInfo(b))
                    .OrderByDescending(b => b.Version)
                    .ToList();
        }


        /// <summary>
        /// Find information about all 'MAJOR.MINOR-latest' branches, in decreasing version order
        /// </summary>
        ///
        IList<MajorMinorLatestBranchInfo> FindMajorMinorLatestBranches()
        {
            return
                GitRepository.GetBranches()
                    .Where(b => MajorMinorLatestBranchInfo.IsMajorMinorLatestBranchName(b.Name))
                    .Select(b => new MajorMinorLatestBranchInfo(b))
                    .OrderByDescending(b => b.Version)
                    .ToList();
        }


        SemVersion GetMasterBranchVersion(GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (@ref.IsBranch) throw new ArgumentException("Not a branch", nameof(@ref));

            if (@ref.Name == "master")
            {
                return
                    CalculateReleaseVersion(GetCommit(@ref.Target))
                        .Change(null, null, 0, "", "");
            }

            var match = Regex.Match(@ref.Name, @"^(\d+)\.(\d+)-master$");
            if (match.Success)
            {
                return new SemVersion(
                    int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
            }

            return null;
        }

    }
}
