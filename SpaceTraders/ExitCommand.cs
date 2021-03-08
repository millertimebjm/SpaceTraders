using System;
using System.Collections.Generic;
using RestSharp;
using Newtonsoft.Json;

namespace SpaceTraders
{
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
        public bool CanExecute(SpaceTraderStateModel state)
        {
            return true;
        }
    }
}