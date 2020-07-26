using System;
using System.Collections.Generic;
using MacroExceptions;

namespace Verbot.Commands
{
    class ReadCommand : ICommand
    {

        readonly Context Context;


        public ReadCommand(Context context)
        {
            Context = context;
        }


        public int Run(Queue<string> args)
        {
            if (args.Count > 0)
            {
                throw new UserException("Unexpected arguments");
            }

            var version = Context.DiskLocationContext.ReadVersion();
            Console.Out.WriteLine(version);

            return 0;
        }

    }
}
