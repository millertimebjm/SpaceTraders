using System;
using System.Collections.Generic;
using SpaceTraders.Models;
using SpaceTraders.Commands;

namespace SpaceTraders
{
    public class SpaceTraderService : ISpaceTrader
    {
        private SpaceTraderStateModel _state = new SpaceTraderStateModel();
        private Dictionary<string, ICommand> commandDictionary = new Dictionary<string, ICommand>();
        public SpaceTraderService()
        {
            //_state.Token = "81027fa0-d927-49b4-af77-e726b364a6c2";
            _state.Token = "77573311-bbe9-4fc0-9143-0577d4d01b7b";
            _state.User = new User()
            {
                Id = "cklvs4ky928836em89qtw9qdty",
                Username = "millertimebjm",
            };
            foreach (var command in CommandFactory.GetCommands())
            {
                foreach (var commandString in command.GetInputs())
                {
                    commandDictionary.Add(commandString, command);
                }
            }
        }
        public void Run()
        {
            Console.WriteLine("Welcome to Space Traders.");
            Console.WriteLine();
            DisplayCommands();
            Console.WriteLine();
            //GetUsername();
            ICommand statusCommand = new StatusCommand();
            statusCommand.Execute(_state);
            //ICommand tokenCommand = new TokenCommand();
            //tokenCommand.Execute(_state);
            do
            {
                Console.WriteLine();
                Console.Write(" > ");
                var commandString = Console.ReadLine();

                try
                {
                    var command = GetCommand(commandString);
                    if (command != null)
                    {
                        command.Execute(_state);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    Console.WriteLine();
                }
            } while (!_state.Complete);
        }

        private void DisplayCommands()
        {
            Console.WriteLine("Listing Commands...");
            foreach (var command in CommandFactory.GetCommands())
            {
                Console.WriteLine($"{command.Name} [{string.Join(",", command.GetInputs())}]");
            }
            Console.WriteLine("Complete.");
        }

        //private void GetUsername()
        //{
        //    //Console.Write($"Username ({_state.User.Username}?): ");
        //    var input = Console.ReadLine();
        //    if (!string.IsNullOrWhiteSpace(input))
        //    {
        //        _state.User.Username = input;
        //    }
        //    Console.WriteLine($"Using {_state.User.Username}.");
        //}

        private ICommand GetCommand(string input)
        {
            if (commandDictionary.ContainsKey(input.ToLower().Trim()))
            {
                return commandDictionary[input.ToLower().Trim()];
            }
            return null;
        }
    }
}