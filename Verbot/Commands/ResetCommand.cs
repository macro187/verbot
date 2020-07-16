using System.Collections.Generic;
using MacroExceptions;

namespace Verbot.Commands
{
    class ResetCommand : ICommand
    {

        readonly Context Context;


        public ResetCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            if (args.Count > 0)
            {
                throw new UserException("Unexpected arguments");
            }

            Context.DiskLocationContext.WriteVersion(Context.DefaultVersion);

            return 0;
        }

    }
}
