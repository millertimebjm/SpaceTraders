using System;
using System.Collections.Generic;

namespace SpaceTraders.Commands
{
    public static class CommandFactory
    {
        public static IEnumerable<ICommand> GetCommands(IRestApi restApi)
        {
            return new List<ICommand>
            {
                new TokenCommand(),
                new StatusCommand(),
                new AccountCommand(restApi),
                new ExitCommand(),
                new LoanShowCommand(),
                new LoanTakeCommand(),
            };
        }
    }
}