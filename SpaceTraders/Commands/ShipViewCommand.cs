using Newtonsoft.Json;
using RestSharp;
using SpaceTraders.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpaceTraders.Commands
{
    public class ShipViewCommand : ICommand
    {
        public string Name => "ShipView";

        public bool CanExecute(SpaceTraderStateModel state)
        {
            return true;
        }

        public void Execute(SpaceTraderStateModel state)
        {
            Console.WriteLine("Getting Account Info...");
            var client = new RestClient("https://api.spacetraders.io");
            var request = new RestRequest($"/game/ships?token={state.Token}&{state.CommandParameters.First()}", Method.GET);
            var response = client.Execute(request);
            var accountResponse = JsonConvert.DeserializeObject<AccountResponse>(response.Content);
            state.User = accountResponse.User;
            Console.WriteLine(state.User.ToString());
            Console.WriteLine("Completed.");
        }

        public IEnumerable<string> GetInputs()
        {
            return new List<string>()
            {
                "ship view [name]",
                "sv [name]",
            };
        }
    }
}
