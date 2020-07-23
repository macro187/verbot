using System.Collections.Generic;
using System.Diagnostics;
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

            var failure = Context.CheckContext.CheckLocal();
            if (failure != null)
            {
                Trace.TraceError(failure.Description);
                Trace.TraceInformation(failure.RepairDescription);
                return 1;
            }

            return 0;
        }

    }
}
