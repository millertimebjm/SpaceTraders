using System;
using System.Collections.Generic;
using RestSharp;
using Newtonsoft.Json;
using SpaceTraders.Models;

namespace SpaceTraders.Commands
{
    public class TokenCommand : ICommand
    {
        public string Name 
        { 
            get 
            {
                return "Token";
            }
        }
        public void Execute(SpaceTraderStateModel state)
        {
           Console.WriteLine($"Generating token...");
           var client = new RestClient("https://api.spacetraders.io");
           var request = new RestRequest($"/users/{state.User.Username}/token", Method.POST);
           var response = client.Execute(request);
           var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response.Content);
           state.User = tokenResponse.User;
           state.Token = tokenResponse.Token;
           Console.WriteLine($"Token generated: {state.Token}");
        }
        public bool CanExecute(SpaceTraderStateModel state) 
        {
           return string.IsNullOrWhiteSpace(state.Token) 
                && !string.IsNullOrWhiteSpace(state.User.Username);
        }
        public IEnumerable<string> GetInputs()
        {
            return new List<string>
            {
                "token",
            };
        }
    }
}