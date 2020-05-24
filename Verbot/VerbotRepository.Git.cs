using System.Linq;
using MacroGit;
using MacroSemver;
using System.Collections.Generic;
using System;
using MacroGuards;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Verbot
{
    partial class VerbotRepository
    {

        IEnumerable<GitRefWithRemote> FindReleaseTagsWithRemote()
        {
            var verbotTags = FindReleaseTags();
            var remoteRefsLookup = GitRepository.GetRemoteRefs().ToDictionary(r => r.FullName, r => r.Target);

            GitSha1 LookupRemoteTarget(GitFullRefName fullName) =>
                remoteRefsLookup.TryGetValue(fullName, out var target) ? target : null;

            return
                verbotTags
                    .Select(t => new GitRefWithRemote(t.Ref, LookupRemoteTarget(t.FullName)))
                    .ToList();
        }


        IEnumerable<GitRefWithRemote> GetVerbotBranchesWithRemote()
        {
            var verbotBranches = GetVerbotBranches();
            var remoteBranchesLookup = GitRepository.GetRemoteBranches().ToDictionary(b => b.FullName, b => b.Target);

            GitSha1 LookupRemoteTarget(GitFullRefName fullName) =>
                remoteBranchesLookup.TryGetValue(fullName, out var target) ? target : null;

            return
                verbotBranches
                    .Select(b => new GitRefWithRemote(b, LookupRemoteTarget(b.FullName)))
                    .ToList();
        }


        IEnumerable<ReleaseTagInfo> FindReleaseTags()
        {
            return
                GitRepository.GetTags()
                    .Select(tag =>
                    {
                        SemVersion.TryParse(tag.Name, out var version, true);
                        return (Ref: tag, Version: version);
                    })
                    .Where(tag => tag.Version != null)
                    .Where(tag => tag.Version.Prerelease == "")
                    .Where(tag => tag.Version.Build == "")
                    .Select(tag => new ReleaseTagInfo(tag.Ref, tag.Version))
                    .OrderByDescending(tag => tag.Version)
                    .ToList();
        }


        IEnumerable<GitRef> GetVerbotBranches()
        {
            return
                GitRepository.GetBranches()
                    .Where(b =>
                        IsMasterBranch(b) ||
                        b.Name == "latest" ||
                        MajorLatestBranchInfo.IsMajorLatestBranchName(b.Name) ||
                        MajorMinorLatestBranchInfo.IsMajorMinorLatestBranchName(b.Name))
                    .ToList();
        }


        /// <summary>
        /// Find information about all 'master' and 'MAJOR.MINOR-master' branches, in decreasing order of tracked minor
        /// version
        /// </summary>
        ///
        IList<MasterBranchInfo> FindMasterBranches()
        {
            return
                GitRepository.GetBranches()
                    .Select(b => (Ref: b, Version: GetMasterBranchVersion(b)))
                    .Where(b => b.Version != null)
                    .Select(b => new MasterBranchInfo(b.Ref, b.Version))
                    .OrderByDescending(b => b.Version)
                    .ToList();
        }


        bool IsMasterBranch(GitRef @ref)
        {
            if (!@ref.IsBranch) return false;
            return GetMasterBranchVersion(@ref) != null;
        }


        SemVersion GetMasterBranchVersion(GitRef @ref)
        {
            Guard.NotNull(@ref, nameof(@ref));
            if (@ref.IsBranch) throw new ArgumentException("Not a branch", nameof(@ref));

            if (@ref.Name == "master")
            {
                return CalculateReleaseVersion(@ref.Target, false).Change(null, null, 0, "", "");
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


        class GitRefWithRemote
        {
            public GitRefWithRemote(GitRef @ref, GitSha1 remoteTarget)
            {
                Ref = @ref;
                RemoteTarget = remoteTarget;
            }

            public GitRef Ref { get; }
            public GitRefNameComponent Name => Ref.Name;
            public GitFullRefName FullName => Ref.FullName;
            public GitSha1 Target => Ref.Target;
            public bool IsBranch => Ref.IsBranch;
            public bool IsTag => Ref.IsTag;
            public GitSha1 RemoteTarget { get; }
        }


        class ReleaseTagInfo
        {
            public ReleaseTagInfo(GitRef @ref, SemVersion version)
            {
                if (!@ref.IsTag) throw new ArgumentException("Not a tag", nameof(@ref));
                Ref = @ref;
                Version = version;
            }

            public GitRef Ref { get; }
            public GitRefNameComponent Name => Ref.Name;
            public GitFullRefName FullName => Ref.FullName;
            public GitSha1 Target => Ref.Target;
            public SemVersion Version { get; }
        }

    }
}
