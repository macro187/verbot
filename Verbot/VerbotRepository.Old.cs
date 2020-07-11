using System.Linq;
using MacroExceptions;
using System.Diagnostics;
using MacroGit;
using System.Collections.Generic;

namespace Verbot
{
    partial class VerbotRepository
    {

        public void Push(bool dryRun)
        {
            CheckNoUncommittedChanges();

            var verbotBranchesWithRemote = GetVerbotBranchesWithRemote();
            var verbotTagsWithRemote = FindReleaseTagsWithRemote();

            CheckForRemoteBranchesAtUnknownCommits();
            CheckForRemoteBranchesNotBehindLocal();
            CheckForIncorrectRemoteTags();

            var branchesToPush = verbotBranchesWithRemote.Where(b => b.RemoteTargetSha1 != b.Target.Sha1);
            var tagsToPush = verbotTagsWithRemote.Where(b => b.RemoteTargetSha1 != b.Target.Sha1);
            var refsToPush = branchesToPush.Concat(tagsToPush);

            if (!refsToPush.Any())
            {
                Trace.TraceInformation("All remote version branches and tags already up-to-date");
                return;
            }

            GitRepository.Push(refsToPush.Select(r => r.FullName), dryRun: dryRun, echoOutput: true);
        }


        void CheckForIncorrectRemoteTags()
        {
            var verbotTagsWithRemote = FindReleaseTagsWithRemote();

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


        void CheckForRemoteBranchesAtUnknownCommits()
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


        void CheckForRemoteBranchesNotBehindLocal()
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
            Branches
                .Where(b =>
                    CalculateMasterBranchSeries(b) != null ||
                    b.Name == "latest" ||
                    MajorLatestBranchInfo.IsMajorLatestBranchName(b.Name) ||
                    MajorMinorLatestBranchInfo.IsMajorMinorLatestBranchName(b.Name))
                .ToList();


        public IEnumerable<GitRefWithRemote> GetVerbotBranchesWithRemote() =>
            GetRemoteInfo(VerbotBranches).ToList();

    }
}
