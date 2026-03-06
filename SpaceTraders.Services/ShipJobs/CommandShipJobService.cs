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
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
    
        var (_, shipType) = await _shipCommandHelperService.ShipToBuy(ships);
        if (shipType is not null)
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.PurchaseShip);
        }

        // if (waypoints.Any(w => w.JumpGate is not null && w.IsUnderConstruction))
        // {
        //     if (waypoints.Any(w => !WaypointsService.IsMarketplaceVisited(w)))
        //     {
        //         return new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration);
        //     }
        // }

        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }

    private async Task<bool> IsPurchaseShip(IEnumerable<Ship> ships)
    {
        var agent = await _agentsService.GetAsync();
        var headquartersSystem = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters));
        
        if (agent.Credits > INITIAL_SURVEYOR_SHIP_CREDITS_THRESHOLD)
        {
            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.SURVEYOR.ToString()) < 1) return true;
        }

        if (agent.Credits < PURCHASE_SHIP_CREDITS_THRESHOLD) return false;

        if (headquartersSystem.Waypoints.Any(w => w.JumpGate is null && w.IsUnderConstruction))
        {
            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.SURVEYOR.ToString()) < 1)
            {
                return true;
            }

            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.EXCAVATOR.ToString()) < 9)
            {
                return true;
            }

            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()) < 5)
            {
                return true;
            }
        }
        
        if (headquartersSystem.Waypoints.Any(w => w.JumpGate is not null && !w.IsUnderConstruction))
        {
            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()) < 10)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsExplorationAvailableOutsideCurrentSystem(string systemSymbol, List<Waypoint> waypoints)
    {
        var jumpGate = waypoints.SingleOrDefault(w =>
            WaypointsService.ExtractSystemFromWaypoint(w.Symbol) == systemSymbol
            && w.Type == WaypointTypesEnum.JUMP_GATE.ToString()
            && w.JumpGate is not null
            && !w.IsUnderConstruction);
        if (jumpGate is not null)
        {
            var jumpSystems = jumpGate.JumpGate.Connections;
            foreach (var jumpSystem in jumpSystems)
            {
                var jumpGateConnection = waypoints.SingleOrDefault(w => 
                    WaypointsService.ExtractSystemFromWaypoint(w.Symbol) == WaypointsService.ExtractSystemFromWaypoint(jumpSystem)
                    && w.Type == WaypointTypesEnum.JUMP_GATE.ToString()
                    && w.JumpGate is not null
                    && !w.IsUnderConstruction);
                if (jumpGateConnection is not null)
                {
                    var cacheSystem = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(jumpSystem));
                    if (cacheSystem.Waypoints.Any(w => w.Traits is null || !w.Traits.Any()))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool IsExplorationAvailableInCurrentSystem(string systemSymbol, List<Waypoint> waypoints)
    {
        var unchartedWaypoints =
            waypoints
                .Where(w => !WaypointsService.IsVisited(w) || w.Traits?.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()) == true)
                .Select(w => w.Symbol)
                .ToList();
        return unchartedWaypoints.Any(uw => uw.Contains(systemSymbol));
    }
}