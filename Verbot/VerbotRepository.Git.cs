using System.Linq;
using MacroGit;
using MacroSemver;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Verbot
{
    partial class VerbotRepository
    {

        IEnumerable<GitRefWithRemote> GetVerbotTagsWithRemote()
        {
            var verbotTags = FindReleaseTags();
            var remoteTagsLookup = GitRepository.GetRemoteTags().ToDictionary(t => t.Name, t => t.Id);

            GitCommitName LookupRemoteId(GitCommitName name) =>
                remoteTagsLookup.TryGetValue(name, out var id) ? id : null;

            return
                verbotTags
                    .Select(t => new GitRefWithRemote(t.Name, t.Id, LookupRemoteId(t.Name)))
                    .ToList();
        }


        IEnumerable<GitRefWithRemote> GetVerbotBranchesWithRemote()
        {
            var verbotBranches = GetVerbotBranches();
            var remoteBranchesLookup = GitRepository.GetRemoteBranches().ToDictionary(b => b.Name, b => b.Id);

            GitCommitName LookupRemoteId(GitCommitName name) =>
                remoteBranchesLookup.TryGetValue(name, out var id) ? id : null;

            return
                verbotBranches
                    .Select(t => new GitRefWithRemote(t.Name, t.Id, LookupRemoteId(t.Name)))
                    .ToList();
        }


        IEnumerable<ReleaseTagInfo> FindReleaseTags()
        {
            return
                GitRepository.GetTags()
                    .Where(t => IsReleaseVersionNumber(t.Name))
                    .Select(t => new ReleaseTagInfo(t.Name, SemVersion.Parse(t.Name), t.Id))
                    .OrderByDescending(t => t.Version)
                    .ToList();
        }


        IEnumerable<GitRef> GetVerbotBranches()
        {
            return
                GitRepository.GetBranches()
                    .Where(b =>
                        MasterBranchInfo.IsMasterBranchName(b.Name) ||
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
                    .Where(b => MasterBranchInfo.IsMasterBranchName(b.Name))
                    .Select(b => new MasterBranchInfo(this, b.Name))
                    .OrderByDescending(b => b.Version)
                    .ToList();
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
                    .Select(b => new MajorLatestBranchInfo(this, b.Name))
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
                    .Select(b => new MajorMinorLatestBranchInfo(this, b.Name))
                    .OrderByDescending(b => b.Version)
                    .ToList();
        }


        static bool IsReleaseVersionNumber(string value)
        {
            return Regex.IsMatch(value, @"^\d+\.\d+\.\d+$");
        }


        class GitRefWithRemote
        {
            public GitRefWithRemote(GitCommitName name, GitCommitName localId, GitCommitName remoteId)
            {
                Name = name;
                LocalId = localId;
                RemoteId = remoteId;
            }

            public GitCommitName Name { get; }
            public GitCommitName LocalId { get; }
            public GitCommitName RemoteId { get; }
        }


        class ReleaseTagInfo
        {
            public ReleaseTagInfo(GitCommitName name, SemVersion version, GitCommitName id)
            {
                Name = name;
                Version = version;
                Id = id;
            }

            public GitCommitName Name { get; }
            public SemVersion Version { get; }
            public GitCommitName Id { get; }
        }

    }
}
