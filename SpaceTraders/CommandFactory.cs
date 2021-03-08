using System;
using System.Collections.Generic;

namespace SpaceTraders 
{
    public static class CommandFactory
    {
        public static IEnumerable<ICommand> GetCommands()
        {
            return new List<ICommand>
            {
                new StatusCommand(),
                new AccountCommand(),
                new ExitCommand(),
                new LoanShowCommand(),
            };
        }
    }
}