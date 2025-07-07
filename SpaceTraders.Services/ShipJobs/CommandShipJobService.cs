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

    public async Task<ShipCommand> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        // var agent = await _agentsService.GetAsync();
        // if (agent.Credits > 900000)
        // {
        //     var shipTypes = ships
        //         .Where(s => s.Nav.SystemSymbol == ship.Nav.SystemSymbol)
        //         .GroupBy(s => s.Frame.Symbol);
        //     if (shipTypes.Count(st => st.Key == ShipTypesEnum.FRAME_DRONE.ToString()) < 5
        //         && shipTypes.Count(st => st.Key == ShipTypesEnum.FRAME_LIGHT_FREIGHTER.ToString()) < 5)
        //     {
        //         return new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.PurchaseShip, "");    
        //     }
        // }
        return new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.BuyToSell, "X1-MD38-H48");
    }
}