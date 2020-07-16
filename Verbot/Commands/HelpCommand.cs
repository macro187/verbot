using System.Collections.Generic;
using MacroExceptions;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using MacroConsole;
using MacroIO;

namespace Verbot.Commands
{
    class HelpCommand : ICommand
    {

        public int Run(Queue<string> args)
        {
            if (args.Count > 0) throw new UserException("Unexpected arguments");

            Trace.TraceInformation("");
            using (var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("Verbot.readme.md"))
            using (var reader = new StreamReader(stream))
            {
                foreach (
                    var line
                    in ReadmeFilter.SelectSections(
                        reader.ReadAllLines(),
                        "Synopsis",
                        "Description",
                        "Commands"
                        ))
                {
                    Trace.TraceInformation(line);
                }
            }

            return 0;
        }

    }
}
