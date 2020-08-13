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

            while (true)
            {
                var failure = Context.CheckContext.Check();
                if (failure == null) return 0;

                Trace.TraceError(failure.Description);
                if (failure.Repair != null)
                {
                    Trace.TraceInformation("Repair command can fix this by:");
                }
                Trace.TraceInformation(failure.RepairDescription);

                return 1;
            }
        }

    }
}
