using System.Collections.Generic;
using System.Diagnostics;
using MacroExceptions;

namespace Verbot.Commands
{
    class RepairCommand : ICommand
    {

        readonly Context Context;


        public RepairCommand(Context context)
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

                if (failure.Repair == null)
                {
                    Trace.TraceError(failure.Description);
                    Trace.TraceInformation(failure.RepairDescription);
                    return 1;
                }

                Trace.TraceInformation(failure.Description);
                Trace.TraceInformation(failure.RepairDescription);
                failure.Repair();
                Context.ResetContexts();
            }
        }

    }
}
