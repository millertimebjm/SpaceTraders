using System;
using System.Collections.Generic;
using RestSharp;
using Newtonsoft.Json;
using SpaceTraders.Models;

namespace SpaceTraders.Commands
{
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
        public bool CanExecute(SpaceTraderStateModel state)
        {
            return true;
        }
    }
}
   