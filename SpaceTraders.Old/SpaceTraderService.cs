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
        private readonly IRestApi _restApi = new SpaceTradersRestApi("https://api.spacetraders.io");
        private readonly ICredentials _credentials = new FileCredentials("Credentials.txt");
        public SpaceTraderService()
        {
            var model = _credentials.Get();

            _state.Token = model.Token;
            _state.User = new User()
            {
                Username = model.Username,
            };
            foreach (var command in CommandFactory.GetCommands(_restApi))
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
            if (string.IsNullOrWhiteSpace(_state.Token))
            {
                RequestUsername();
                ICommand command = new TokenCommand();
                command.Execute(_state);
                _credentials.Save(new CredentialsModel()
                {
                    Username = _state.User.Username,
                    Token = _state.Token,
                });
            }
            else
            {
                //ICommand command = new AccountCommand(_restApi);
                //command.Execute(_state);
                //if ()
            }
            DisplayStatus();
            CommandLoop();
        }

        private void DisplayStatus()
        {
            ICommand statusCommand = new StatusCommand();
            statusCommand.Execute(_state);
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
            foreach (var command in CommandFactory.GetCommands(null))
            {
                Console.WriteLine($"{command.Name} [{string.Join(",", command.GetInputs())}]");
            }
        }

        private void RequestUsername()
        {
           Console.Write($"Username [{_state.User.Username}]: ");
           var input = Console.ReadLine();
           if (!string.IsNullOrWhiteSpace(input))
           {
               _state.User.Username = input;
           }
           Console.WriteLine($"Using [{_state.User.Username}].");
        }

        private ICommand GetCommand(string input)
        {
            var inputTemp = input;
            while (input.Length > 0)
            {
                var key = commandDictionary.Keys.SingleOrDefault(_ => inputTemp.ToLower().Trim() == _);
                if (key != null)
                {
                    _state.CommandKey = key;
                    _state.CommandParameters = input.Replace(key, "").Trim().Split(" ");
                    return commandDictionary[key];
                }
                inputTemp = RemoveLastWord(inputTemp);
            }
            return null;
        }

        private string RemoveLastWord(string input)
        {
            var inputArray = input.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
            inputArray.Remove(inputArray.Last());
            return string.Join(" ", inputArray);
        }
    }
}