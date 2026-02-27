using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class TransportShipJobService(
    IAgentsService _agentsService,
    ISystemsService _systemsService,
    IWaypointsService _waypointsService
) : IShipJobService
{
    public async Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        
        if (IsExplorationAvailableInCurrentSystem(ship.Nav.SystemSymbol, waypoints))
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration);
        }

        if (await IsExplorationAvailableOutsideCurrentSystem(ship.Nav.SystemSymbol, waypoints))
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration);
        }

        return null;
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