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
        
        IEnumerable<GitRef> RefsCache;
        IEnumerable<ReleaseTagInfo> ReleaseTagsCache;
        IEnumerable<MasterBranchInfo> MasterBranchesCache;


        public IEnumerable<GitRef> Refs =>
            RefsCache ?? (RefsCache =
                GitRepository.GetRefs().ToList());


        public IEnumerable<GitRef> Tags =>
            Refs.Where(r => r.IsTag);
            

        public IEnumerable<GitRef> Branches =>
            Refs.Where(r => r.IsBranch);


        public IEnumerable<ReleaseTagInfo> ReleaseTags =>
            ReleaseTagsCache ?? (ReleaseTagsCache =
                Tags
                    .Select(tag =>
                    {
                        SemVersion.TryParse(tag.Name, out var version, true);
                        return (Ref: tag, Version: version);
                    })
                    .Where(tag => tag.Version != null)
                    .Where(tag => tag.Version.Prerelease == "")
                    .Where(tag => tag.Version.Build == "")
                    .Select(tag => new ReleaseTagInfo(tag.Ref, tag.Version, GetCommit(tag.Ref.Target)))
                    .OrderByDescending(tag => tag.Version)
                    .ToList());


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


        public IEnumerable<GitRefWithRemote> FindReleaseTagsWithRemote() =>
            GetRemoteInfo(ReleaseTags.Select(t => t.Ref)).ToList();


        public IEnumerable<GitRefWithRemote> GetVerbotBranchesWithRemote() =>
            GetRemoteInfo(VerbotBranches).ToList();


        IEnumerable<GitRefWithRemote> GetRemoteInfo(IEnumerable<GitRef> refs)
        {
            var remoteRefs = GitRepository.GetRemoteRefs().ToDictionary(r => r.FullName, r => r.Target);

            GitSha1 LookupRemoteTarget(GitFullRefName fullName) =>
                remoteRefs.TryGetValue(fullName, out var target) ? target : null;

            return refs.Select(r => new GitRefWithRemote(r, LookupRemoteTarget(r.FullName)));
        }


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
