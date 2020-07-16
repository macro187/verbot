using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MacroExceptions;
using MacroGit;
using Verbot.LatestBranches;
using Verbot.MasterBranches;
using Verbot.Refs;
using Verbot.Releases;

namespace Verbot
{
    partial class Context
    {

        public void CheckNoUncommittedChanges()
        {
            if (GitRepository.HasUncommittedChanges())
                throw new UserException("Uncommitted changes in repository");
        }


        public void CheckForIncorrectRemoteTags()
        {
            var verbotTagsWithRemote = ReleaseContext.FindReleaseTagsWithRemote();

            var incorrectRemoteTags =
                verbotTagsWithRemote
                    .Where(t => t.RemoteTargetSha1 != null)
                    .Where(t => t.RemoteTargetSha1 != t.Target.Sha1)
                    .ToList();

            if (!incorrectRemoteTags.Any()) return;

            foreach (var tag in incorrectRemoteTags)
            {
                Trace.TraceError($"Remote tag {tag.Name} at {tag.RemoteTargetSha1} local {tag.Target.Sha1}");
            }

            throw new UserException("Incorrect remote tag(s) found");
        }


        public void CheckForRemoteBranchesAtUnknownCommits()
        {
            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();

            var remoteBranchesAtUnknownCommits =
                verbotBranchesWithRemote
                    .Where(b => b.RemoteTargetSha1 != null)
                    .Where(b => !GitRepository.Exists(b.RemoteTargetSha1))
                    .ToList();

            if (!remoteBranchesAtUnknownCommits.Any()) return;

            foreach (var branch in remoteBranchesAtUnknownCommits)
            {
                Trace.TraceError($"Remote branch {branch.Name} at unknown commit {branch.RemoteTargetSha1}");
            }

            throw new UserException("Remote branch(es) at unknown commits");
        }


        public void CheckForRemoteBranchesNotBehindLocal()
        {
            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();

            var remoteBranchesNotBehindLocal =
                verbotBranchesWithRemote
                    .Where(b => b.RemoteTargetSha1 != null)
                    .Where(b => !GitRepository.IsAncestor(b.RemoteTargetSha1, b.Target.Sha1))
                    .ToList();

            if (!remoteBranchesNotBehindLocal.Any()) return;

            foreach (var branch in remoteBranchesNotBehindLocal)
            {
                Trace.TraceError(
                    $"Remote branch {branch.Name} at {branch.RemoteTargetSha1} not behind local at {branch.Target.Sha1}");
            }

            throw new UserException("Remote branch(es) not behind local");
        }


        IEnumerable<RefInfo> VerbotBranches =>
            RefContext.Branches
                .Where(b =>
                    MasterBranchContext.CalculateMasterBranchSeries(b) != null ||
                    b.Name == "latest" ||
                    MajorLatestBranchInfo.IsMajorLatestBranchName(b.Name) ||
                    MajorMinorLatestBranchInfo.IsMajorMinorLatestBranchName(b.Name))
                .ToList();


        public IEnumerable<GitRefWithRemote> GetVerbotBranchesWithRemote() =>
            RefContext.GetRemoteInfo(VerbotBranches).ToList();

    }
}
