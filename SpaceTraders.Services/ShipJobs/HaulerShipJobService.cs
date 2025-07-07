using SpaceTraders.Models;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class HaulerShipJobService : IShipJobService
{
    private readonly ISystemsService _systemsService;
    public HaulerShipJobService(
        ISystemsService systemsService)
    {
        _systemsService = systemsService;
    }

    public async Task<ShipCommand> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var unfinishedJumpGateWaypoint = system.Waypoints.SingleOrDefault(w => w.JumpGate is not null && w.IsUnderConstruction);

        if (unfinishedJumpGateWaypoint is not null 
            && !ships.Where(s => s.Symbol != ship.Symbol).Any(s => s.ShipCommand?.ShipCommandEnum == Models.Enums.ShipCommandEnum.SupplyConstruction))
        {
            return new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.SupplyConstruction, unfinishedJumpGateWaypoint.Symbol);
        }
        return new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.BuyToSell, "X1-NA85-E49");
    }
}