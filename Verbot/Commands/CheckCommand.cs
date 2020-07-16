using System.Collections.Generic;
using MacroExceptions;

namespace Verbot.Commands
{
    class CheckCommand : ICommand
    {

        readonly Context Context;


        public CheckCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");
            Context.CheckContext.CheckLocal();
            return 0;
        }

    }
}
