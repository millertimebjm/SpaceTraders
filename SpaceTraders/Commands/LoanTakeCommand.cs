using System;
using System.Collections.Generic;
using RestSharp;
using Newtonsoft.Json;
using SpaceTraders.Models;
using System.Linq;

namespace SpaceTraders.Commands
{
    public class LoanTakeCommand : ICommand
    {
        public string Name
        {
            get
            {
                return "LoanTake";
            }
        }
        public void Execute(SpaceTraderStateModel state)
        {
            Console.WriteLine("Taking out loan...");
            if (string.IsNullOrWhiteSpace(state.CommandParameters.FirstOrDefault()))
            {
                Console.WriteLine("Error: Loan Name not available.");
                return;
            }
            var client = new RestClient("https://api.spacetraders.io");
            var request = new RestRequest($"/users/{state.User.Username}/loans?token={state.Token}&type={state.CommandParameters.First()}", Method.POST);
            var response = client.Execute(request);
            var userResponse = JsonConvert.DeserializeObject<User>(response.Content);
            Console.WriteLine(userResponse.ToString());
            Console.WriteLine("Completed.");
        }
        public IEnumerable<string> GetInputs()
        {
            return new List<string>
            {
                "lt [name]",
                "loan take [name]",
            };
        }
        public bool CanExecute(SpaceTraderStateModel state)
        {
            return true;
        }
    }
}