using System.Collections.Generic;
using MacroExceptions;

namespace Verbot.Commands
{
    class ReleaseCommand : ICommand
    {

        readonly Context Context;


        public ReleaseCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");
            Context.ReleaseContext.Release();
            return 0;
        }

    }
}
