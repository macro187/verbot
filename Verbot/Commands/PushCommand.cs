using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using MacroExceptions;

namespace Verbot.Commands
{
    class PushCommand : ICommand
    {

        readonly Context Context;


        public PushCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            bool dryRun = false;
            while (args.Count > 0)
            {
                var arg = args.Dequeue();
                switch (arg)
                {
                    case "--dry-run":
                        dryRun = true;
                        break;
                    default:
                        throw new UserException($"Unrecognised argument: {arg}");
                }
            }

            Context.CheckNoUncommittedChanges();

            var verbotBranchesWithRemote = Context.GetVerbotBranchesWithRemote();
            var verbotTagsWithRemote = Context.ReleaseContext.FindReleaseTagsWithRemote();

            Context.CheckForRemoteBranchesAtUnknownCommits();
            Context.CheckForRemoteBranchesNotBehindLocal();
            Context.CheckForIncorrectRemoteTags();

            var branchesToPush = verbotBranchesWithRemote.Where(b => b.RemoteTargetSha1 != b.Target.Sha1);
            var tagsToPush = verbotTagsWithRemote.Where(b => b.RemoteTargetSha1 != b.Target.Sha1);
            var refsToPush = branchesToPush.Concat(tagsToPush);

            if (!refsToPush.Any())
            {
                Trace.TraceInformation("All remote version branches and tags already up-to-date");
                return 0;
            }

            Context.GitRepository.Push(refsToPush.Select(r => r.FullName), dryRun: dryRun, echoOutput: true);
            return 0;
        }

    }
}
