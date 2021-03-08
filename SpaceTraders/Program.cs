using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace SpaceTraders
{
    class Program
    {
        static void Main(string[] args)
        {
            ISpaceTrader spaceTraderService = new SpaceTraderService();
            spaceTraderService.Run();
        }
    }

    interface ISpaceTrader
    {
        void Run();
    }

    public class SpaceTraderService : ISpaceTrader
    {
        private SpaceTraderStateModel _state = new SpaceTraderStateModel();
        private Dictionary<string, ICommand> commandDictionary = new Dictionary<string, ICommand>();
        public SpaceTraderService()
        {
            _state.Token = "81027fa0-d927-49b4-af77-e726b364a6c2";
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

    public class SpaceTraderStateModel
    {
        public User User { get; set; }
        public bool Complete { get; set; } = false;
        public string Status { get; set; } = "";
        public string Token { get; set; } = "";
    }

    public class ExitCommand : ICommand
    {
        public string Name 
        { 
            get 
            {
                return "Exit";
            }
        }
        public void Execute(SpaceTraderStateModel state)
        {
            Console.WriteLine("Thank you for playing.");
            state.Complete = true;
        }
        public IEnumerable<string> GetInputs()
        {
            return new List<string>
            {
                "quit",
                "exit",
            };
        }
    }

    public class StatusCommand : ICommand
    {
        public string Name
        {
            get
            {
                return "Status";
            }
        }
        public void Execute(SpaceTraderStateModel state)
        {
            var client = new RestClient("https://api.spacetraders.io");
            var request = new RestRequest("/game/status", Method.GET);
            var response = client.Execute(request);
            var status = JsonConvert.DeserializeObject<StatusResponse>(response.Content).Status;
            Console.WriteLine(status);
        }
        public IEnumerable<string> GetInputs()
        {
            return new List<string>
            {
                "status",
            };
        }
    }

    //public class TokenCommand : ICommand
    //{
    //    public void Execute(SpaceTraderStateModel state)
    //    {
    //        Console.WriteLine($"Generating token...");
    //        var client = new RestClient("https://api.spacetraders.io");
    //        var request = new RestRequest($"/users/{state.User.Username}/token", Method.POST);
    //        var response = client.Execute(request);
    //        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response.Content);
    //        state.User = tokenResponse.User;
    //        state.Token = tokenResponse.Token;
    //        Console.WriteLine($"Token generated: {state.Token}");
    //    }
    //}
    
    public class AccountCommand : ICommand
    {
        public string Name
        {
            get
            {
                return "Account";
            }
        }
        public void Execute(SpaceTraderStateModel state)
        {
            Console.WriteLine("Getting Account Info...");
            var client = new RestClient("https://api.spacetraders.io");
            var request = new RestRequest($"/users/{state.User.Username}?token={state.Token}", Method.GET);
            var response = client.Execute(request);
            var accountResponse = JsonConvert.DeserializeObject<AccountResponse>(response.Content);
            state.User = accountResponse.User;
            Console.WriteLine(state.User.ToString());
            Console.WriteLine("Completed.");
        }
        public IEnumerable<string> GetInputs()
        {
            return new List<string>
            {
                "account",
                "a",
            };
        }
    }

    public class LoanShowCommand : ICommand
    {
        public string Name
        {
            get
            {
                return "LoanShow";
            }
        }
        public void Execute(SpaceTraderStateModel state)
        {
            Console.WriteLine("Getting Loan Info...");
            var client = new RestClient("https://api.spacetraders.io");
            var request = new RestRequest($"/game/loans/?token={state.Token}", Method.GET);
            var response = client.Execute(request);
            var accountResponse = JsonConvert.DeserializeObject<LoanList>(response.Content);
            Console.WriteLine(string.Join(Environment.NewLine, accountResponse.Loans));
            Console.WriteLine("Completed.");
        }
        public IEnumerable<string> GetInputs()
        {
            return new List<string>
            {
                "ls",
                "loan show",
                "loan",
                "loans",
            };
        }
    }

    interface IResponse
    {

    }
    public class StatusResponse : IResponse
    {
        public string Status { get; set; }
    }
    public class TokenResponse : IResponse
    {
        public string Token { get; set; }
        public User User { get; set; }
    }
    public class User
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string picture { get; set; }
        public string email { get; set; }
        public long Credits { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<Ship> Ships { get; set; }
        public List<Loan> Loans { get; set; }
    }
    public class AccountResponse : IResponse
    {
        public User User { get; set; }
    }
    public class Ship
    {

    }
    public enum LoanStatusEnum {
        Current,
    }
    public class Loan
    {
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public bool CollateralRequired { get; set; }
        public decimal Rate { get; set; }
        public int TerminInDays { get; set; }

        public bool HasLoan { 
            get 
            {
                return !string.IsNullOrWhiteSpace(Id);
            }
        }
        public DateTime Due { get; set; }
        public string Id { get; set; }
        public decimal RepaymentAmount { get; set; }
        public LoanStatusEnum Status { get; set; }

        public override string ToString() 
        {
            var collateralRequiredString = CollateralRequired ? "Y" : "N";
            if (HasLoan) 
            {
                return $"Loan {Status} Due:{Due.ToString("s", CultureInfo.CreateSpecificCulture("en-US"))} | {Type} {RepaymentAmount}({Amount})  Collateral:{collateralRequiredString} {Rate} Term:{TerminInDays}";
            } 
            else 
            {
                return $"Loan | {Type} {Amount} Collateral:{collateralRequiredString} {Rate} Term:{TerminInDays}";
            }
            
        }
    }
    public class LoanList
    {
        public List<Loan> Loans { get; set; }
    }
}

