using System;
using System.Collections.Generic;
using RestSharp;
using Newtonsoft.Json;
using SpaceTraders.Models;

namespace SpaceTraders.Commands
{
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
        public bool CanExecute(SpaceTraderStateModel state)
        {
            return true;
        }
    }
}