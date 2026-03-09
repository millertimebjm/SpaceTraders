using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class CommandShipJobService(
    IAgentsService _agentsService,
    ISystemsService _systemsService,
    IShipCommandsHelperService _shipCommandHelperService,
    IShipyardsService _shipyardsService
) : IShipJobService
{
    private const long INITIAL_SURVEYOR_SHIP_CREDITS_THRESHOLD = 50_000;
    private const long PURCHASE_SHIP_CREDITS_THRESHOLD = 800_000;

    public async Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var agent = await _agentsService.GetAsync();
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
    
        if ((ships.Count() == 2 && agent.Credits > INITIAL_SURVEYOR_SHIP_CREDITS_THRESHOLD)
            || (agent.Credits > PURCHASE_SHIP_CREDITS_THRESHOLD))
        {
            var (shipyardWaypoint, shipType) = await _shipCommandHelperService.ShipToBuy(ships);
            if (shipType is not null && shipyardWaypoint is not null)
            {
                if(!await CheckRemotePurchaseShip(ships, shipyardWaypoint, shipType.Value))
                {
                    return new ShipCommand(ship.Symbol, ShipCommandEnum.PurchaseShip);
                }
            }
        }

        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }

    private async Task<bool> CheckRemotePurchaseShip(IEnumerable<Ship> ships, string shipyardWaypoint, ShipTypesEnum shipType)
    {
        if (ships.Any(s => s.Nav.WaypointSymbol == shipyardWaypoint && s.Nav.Status == NavStatusEnum.DOCKED.ToString()))
        {
            await _shipyardsService.PurchaseShipAsync(shipyardWaypoint, shipType.ToString());
            return true;
        }
        return false;
    }
}