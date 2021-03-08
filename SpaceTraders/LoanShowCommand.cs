using System;
using System.Collections.Generic;
using RestSharp;
using Newtonsoft.Json;

namespace SpaceTraders
{
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
        public bool CanExecute(SpaceTraderStateModel state)
        {
            return true;
        }
    }
}