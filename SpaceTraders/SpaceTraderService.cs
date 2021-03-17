using System;
using System.Collections.Generic;
using SpaceTraders.Models;
using SpaceTraders.Commands;
using System.Text.RegularExpressions;
using System.Linq;

namespace SpaceTraders
{
    public class SpaceTraderService : ISpaceTrader
    {
        private SpaceTraderStateModel _state = new SpaceTraderStateModel();
        private Dictionary<string, ICommand> commandDictionary = new Dictionary<string, ICommand>();
        public SpaceTraderService()
        {
            //_state.Token = "81027fa0-d927-49b4-af77-e726b364a6c2";
            //_state.Token = "77573311-bbe9-4fc0-9143-0577d4d01b7b";
            _state.Token = "e53fd57e-701a-48e2-8cb6-b54635ef86ff";
            _state.User = new User()
            {
                //Id = "cklvs4ky928836em89qtw9qdty",
                Username = "millertimebjm",
            };
            foreach (var command in CommandFactory.GetCommands())
            {
                foreach (var commandString in command.GetInputs())
                {
                    var tempCommandString = Regex.Replace(commandString, @"\[(.*?)\]", "");
                    commandDictionary.Add(tempCommandString.Trim(), command);
                }
            }
        }
        public void Run()
        {
            Console.WriteLine("Welcome to Space Traders.");
            Console.WriteLine();
            DisplayCommands();
            Console.WriteLine();
            VerifyUsername();
            DisplayStatus();
            GetToken();
            CommandLoop();
        }

        private void VerifyUsername()
        {
            if (string.IsNullOrWhiteSpace(_state.User.Username))
            {
                RequestUsername();
            }
        }

        private void DisplayStatus()
        {
            ICommand statusCommand = new StatusCommand();
            statusCommand.Execute(_state);
        }

        private void GetToken()
        {
            if (string.IsNullOrWhiteSpace(_state.Token))
            {
                ICommand tokenCommand = new TokenCommand();
                tokenCommand.Execute(_state);
            }
        }

        private void CommandLoop()
        {
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
        }

        private void RequestUsername()
        {
           //Console.Write($"Username ({_state.User.Username}?): ");
           var input = Console.ReadLine();
           if (!string.IsNullOrWhiteSpace(input))
           {
               _state.User.Username = input;
           }
           Console.WriteLine($"Using {_state.User.Username}.");
        }

        private ICommand GetCommand(string input)
        {
            var key = commandDictionary.Keys.SingleOrDefault(_ => input.ToLower().Trim() == _);
            if (key != null)
            {
                _state.CommandKey = key;
                _state.CommandParameters = input.Replace(key, "").Trim().Split(" ");
                return commandDictionary[key];
            }
            return null;
        }
    }
}