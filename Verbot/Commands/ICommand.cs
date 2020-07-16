using System.Collections.Generic;

namespace Verbot.Commands
{
    interface ICommand
    {

        int Run(Queue<string> args);

    }
}
