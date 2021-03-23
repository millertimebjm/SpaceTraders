using RestSharp;
using SpaceTraders.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpaceTraders.Commands
{
    public class ShipViewCommand : ICommand
    {
        private readonly IRestApi _restApi;
        public ShipViewCommand(IRestApi restApi)
        {
            _restApi = restApi;
        }
        public string Name => "ShipView";
        public bool CanExecute(SpaceTraderStateModel state)
        {
            return true;
        }

        public void Execute(SpaceTraderStateModel state)
        {
            Console.WriteLine("Getting Account Info...");
            var response = _restApi.Execute<ShipList>($"/game/ships?token={state.Token}&class={state.CommandParameters.First()}", Method.GET);
            if (response.Successful)
            {
                foreach (var ship in response.Model.Ships)
                {
                    Console.WriteLine(ship);
                }
            }
            else
            {
                Console.WriteLine("Error: " + response.ErrorResponse);
            }
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
