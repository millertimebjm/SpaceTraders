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

    public class SpaceTraderStateModel
    {
        public User User { get; set; }
        public bool Complete { get; set; } = false;
        public string Status { get; set; } = "";
        public string Token { get; set; } = "";
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
        public string Picture { get; set; }
        public string Email { get; set; }
        public long Credits { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public IEnumerable<Ship> Ships { get; set; } = new List<Ship>();
        public IEnumerable<Loan> Loans { get; set; } = new List<Loan>();

        public override string ToString()
        {
            var result = $@"User {Username} ({Id}) | Credits:{Credits}"; // UpdatedAt:{UpdatedAt.ToString("s", CultureInfo.CreateSpecificCulture("en-US"))}
            if (Ships != null && Ships.Any())
            {
                result += Environment.NewLine + string.Join(Environment.NewLine, Ships.ToString());
            } 
            else 
            {
                result += Environment.NewLine + "Ships: none";
            }
            if (Loans != null && Loans.Any())
            {
                result += Environment.NewLine + string.Join(Environment.NewLine, Loans.ToString());
            }
            else 
            {
                result += Environment.NewLine + "Loans: none";
            }
            return result;
        }
    }
    public class AccountResponse : IResponse
    {
        public User User { get; set; }
    }
    public class Ship
    {
        // General Info
        public string Class { get; set; }
        public string Manufacturer { get; set; }
        public decimal MaxCargo { get; set; }
        public int Plating { get; set; }
        public IEnumerable<PurchaseLocation> PurchaseLocations { get; set; }
        public int Speed { get; set; }
        public string Type { get; set; }
        public int Weapons { get; set; }

        // Owned
        public IEnumerable<Cargo> Cargo { get; set; }
        public string Id { get; set; }
        public string Location { get; set; }
        public decimal SpaceAvailable { get; set; }
    }
    public class Cargo 
    {
        public string Good { get; set; }
        public int Quantity { get; set; }
    }
    public class PurchaseLocation 
    {
        public string Location { get; set; }
        public decimal Price { get; set; }
    }
    public enum LoanStatusEnum {
        Current,
    }
    public class Loan
    {
        // General Info
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public bool CollateralRequired { get; set; }
        public decimal Rate { get; set; }
        public int TermInDays { get; set; }

        public bool HasLoan { 
            get 
            {
                return !string.IsNullOrWhiteSpace(Id);
            }
        }

        // Owned Loan
        public DateTime Due { get; set; }
        public string Id { get; set; }
        public decimal RepaymentAmount { get; set; }
        public LoanStatusEnum Status { get; set; }

        public override string ToString() 
        {
            var collateralRequiredString = CollateralRequired ? "Y" : "N";
            if (HasLoan) 
            {
                return $"Loan {Status} Due:{Due.ToString("s", CultureInfo.CreateSpecificCulture("en-US"))} | {Type} {RepaymentAmount}({Amount})  Collateral:{collateralRequiredString} {Rate} Term:{TermInDays}";
            } 
            else 
            {
                return $"Loan | {Type} {Amount} Collateral:{collateralRequiredString} {Rate} Term:{TermInDays}";
            }
            
        }
    }
    public class LoanList
    {
        public List<Loan> Loans { get; set; }
    }
    public class Order 
    {
        public string Good { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
    }
    public class Location 
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Type { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }
    public class FlightPlan 
    {
        public DateTime ArrivesAt { get; set; }
        public string Destination { get; set; }
        public int FuelConsumed { get; set; }
        public int FuelRemaining { get; set; }
        public string Id { get; set; }
        public string ShipId { get; set; }
        public DateTime? TerminatedAt { get; set; }
        public int TimeRemainingInSeconds { get; set; }
    }
}

