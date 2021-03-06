using System.Collections.Generic;
using MacroExceptions;

namespace Verbot.Commands
{
    class CheckRemoteCommand : ICommand
    {

        readonly Context Context;


        public CheckRemoteCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");

            Context.CheckForRemoteBranchesAtUnknownCommits();
            Context.CheckForRemoteBranchesNotBehindLocal();
            Context.CheckForIncorrectRemoteTags();

            return 0;
        }

    }
}
