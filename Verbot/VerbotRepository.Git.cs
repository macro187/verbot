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

        IDictionary<GitSha1, VerbotCommitInfo> CommitInfoCache =
            new Dictionary<GitSha1, VerbotCommitInfo>();


        VerbotCommitInfo GetHeadCommit() =>
            FindCommit(new GitRev("HEAD")) ?? throw new InvalidOperationException("No HEAD commit");


        VerbotCommitInfo FindCommit(GitRev rev) =>
            GitRepository.TryGetCommitId(rev, out var sha1) ? GetCommit(sha1) : null;


        VerbotCommitInfo GetCommit(GitSha1 sha1)
        {
            if (!CommitInfoCache.ContainsKey(sha1))
            {
                CommitInfoCache.Add(sha1, new VerbotCommitInfo(GitRepository, sha1));
            }

            return CommitInfoCache[sha1];
        }


        IEnumerable<VerbotCommitInfo> GetCommits(IEnumerable<GitSha1> sha1s) =>
            sha1s.Select(s => GetCommit(s));


        IEnumerable<GitRefWithRemote> FindReleaseTagsWithRemote() =>
            GetRemoteInfo(FindReleaseTags().Select(t => t.Ref)).ToList();


        IEnumerable<GitRefWithRemote> GetVerbotBranchesWithRemote() =>
            GetRemoteInfo(GetVerbotBranches()).ToList();


        IEnumerable<GitRefWithRemote> GetRemoteInfo(IEnumerable<GitRef> refs)
        {
            var remoteRefs = GitRepository.GetRemoteRefs().ToDictionary(r => r.FullName, r => r.Target);

            GitSha1 LookupRemoteTarget(GitFullRefName fullName) =>
                remoteRefs.TryGetValue(fullName, out var target) ? target : null;

            return refs.Select(r => new GitRefWithRemote(r, LookupRemoteTarget(r.FullName)));
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
                    .Select(tag => new ReleaseTagInfo(tag.Ref, tag.Version, GetCommit(tag.Ref.Target)))
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
                return
                    CalculateReleaseVersion(GetCommit(@ref.Target), false)
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
            public ReleaseTagInfo(GitRef @ref, SemVersion version, VerbotCommitInfo target)
            {
                Guard.NotNull(@ref, nameof(@ref));
                if (!@ref.IsTag) throw new ArgumentException("Not a tag", nameof(@ref));
                Guard.NotNull(version, nameof(version));
                Guard.NotNull(target, nameof(target));

                Ref = @ref;
                Version = version;
                Target = target;
            }

            public GitRef Ref { get; }
            public SemVersion Version { get; }
            public VerbotCommitInfo Target { get; }
            public GitRefNameComponent Name => Ref.Name;
            public GitFullRefName FullName => Ref.FullName;
        }

    }
}
