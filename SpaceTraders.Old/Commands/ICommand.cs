using System;
using System.Collections.Generic;
using SpaceTraders.Models;

namespace SpaceTraders 
{
    public interface ICommand
    {
        string Name { get; }
        void Execute(SpaceTraderStateModel state);
        IEnumerable<string> GetInputs();
        bool CanExecute(SpaceTraderStateModel state);
    }
}