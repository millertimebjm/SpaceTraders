using System;
using System.Collections.Generic;
using SpaceTraders.Commands;
using SpaceTraders.Models;

namespace SpaceTraders
{
    class Program
    {
        static void Main(string[] args)
        {
            ISpaceTrader spaceTraderService = new SpaceTraderService();
            spaceTraderService.Run();
        }
    }
}

