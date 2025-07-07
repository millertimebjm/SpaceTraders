using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipCommands;
using SpaceTraders.Services.Ships.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class CommandShipJobService : IShipJobService
{
    private readonly IAgentsService _agentsService;
    private readonly IShipsService _shipsService;
    public CommandShipJobService(
        IAgentsService agentsService)
    {
        _agentsService = agentsService;
    }

    public async Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        // var agent = await _agentsService.GetAsync();
        // if (agent.Credits > 900000)
        // {
        //     var shipTypesInSystem = ships
        //         .Where(s => s.Nav.SystemSymbol == ship.Nav.SystemSymbol)
        //         .GroupBy(s => s.Registration.Role);
        //     if (shipTypesInSystem.Count(st => st.Key == ShipTypesEnum.SHIP_MINING_DRONE.ToString()) < 5
        //         || shipTypesInSystem.Count(st => st.Key == ShipTypesEnum.SHIP_LIGHT_HAULER.ToString()) < 5
        //         || shipTypesInSystem.Count(st => st.Key == ShipTypesEnum.SHIP_SURVEYOR.ToString()) == 0)
        //     {
        //         return new ShipCommand(ship.Symbol, ShipCommandEnum.PurchaseShip);    
        //     }
        // }
        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }
}