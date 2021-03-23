using System;
using System.Collections.Generic;
using RestSharp;
using SpaceTraders.Models;

namespace SpaceTraders.Commands
{
    public class AccountCommand : ICommand
    {
        private readonly IRestApi _restApi;
        public AccountCommand(IRestApi restApi)
        {
            _restApi = restApi;
        }
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
            var response = _restApi.Execute<AccountResponse>($"/users/{state.User.Username}?token={state.Token}", Method.GET);
            if (response.Successful)
            {
                state.User = response.Model.User;
                Console.WriteLine(state.User.ToString());
            }
            else
            {
                Console.WriteLine("Error: " + response.ErrorResponse);
            }
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