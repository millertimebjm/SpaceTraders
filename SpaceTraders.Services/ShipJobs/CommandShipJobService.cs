using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class CommandShipJobService(
    IAgentsService _agentsService,
    ISystemsService _systemsService,
    IShipCommandsHelperService _shipCommandHelperService
) : IShipJobService
{
    private const long INITIAL_SURVEYOR_SHIP_CREDITS_THRESHOLD = 50_000;
    private const long PURCHASE_SHIP_CREDITS_THRESHOLD = 800_000;
    private const int MINING_DRONE_MAX_SHIP_COUNT = 9;
    private const int LIGHT_HAULER_MAX_SHIP_COUNT = 5;
    private const int SURVEY_MAX_SHIP_COUNT = 1;
    private const int SHUTTLE_MAX_SHIP_COUNT = 3;

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
            var (_, shipType) = await _shipCommandHelperService.ShipToBuy(ships);
            if (shipType is not null)
            {
                return new ShipCommand(ship.Symbol, ShipCommandEnum.PurchaseShip);
            }
        }

        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }
}