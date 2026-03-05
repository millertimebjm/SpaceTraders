using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class CommandShipJobService(
    IAgentsService _agentsService,
    ISystemsService _systemsService
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
    
        if (await IsPurchaseShip(ships))
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.PurchaseShip);
        }

        if (waypoints.Any(w => w.JumpGate is not null && w.IsUnderConstruction))
        {
            if (waypoints.Any(w => !WaypointsService.IsMarketplaceVisited(w)))
            {
                return new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration);
            }
        }

        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }

    private async Task<bool> IsPurchaseShip(IEnumerable<Ship> ships)
    {
        var agent = await _agentsService.GetAsync();
        if (agent.Credits > INITIAL_SURVEYOR_SHIP_CREDITS_THRESHOLD)
        {
            var shipTypes = ships
                .GroupBy(s => s.Registration.Role);
            var surveyShips = shipTypes.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.SURVEYOR.ToString())?.Count() ?? 0;
            if (surveyShips < SURVEY_MAX_SHIP_COUNT) return true;
        }

        if (agent.Credits > PURCHASE_SHIP_CREDITS_THRESHOLD)
        {
            var shipTypesInSystem = ships
                //.Where(s => s.Nav.SystemSymbol == ship.Nav.SystemSymbol)
                .GroupBy(s => s.Registration.Role);
            var miningDrones = shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.EXCAVATOR.ToString())?.Count() ?? 0;
            var lightHaulers = shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.HAULER.ToString())?.Count() ?? 0;
            
            if (miningDrones < MINING_DRONE_MAX_SHIP_COUNT
                || lightHaulers < LIGHT_HAULER_MAX_SHIP_COUNT)
            {
                return true;
            }
            var shuttles = shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.TRANSPORT.ToString())?.Count() ?? 0;
            var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters));
            var jumpGateWaypoint = system.Waypoints.SingleOrDefault(w => !w.IsUnderConstruction && w.JumpGate is not null);
            if (shuttles < SHUTTLE_MAX_SHIP_COUNT && jumpGateWaypoint is not null)
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