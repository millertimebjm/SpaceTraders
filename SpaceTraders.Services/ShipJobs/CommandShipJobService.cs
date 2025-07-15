using System.Security.Cryptography.X509Certificates;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class CommandShipJobService : IShipJobService
{
    private readonly IAgentsService _agentsService;
    private readonly ISystemsService _systemsService;
    private readonly IWaypointsService _waypointsService;
    public CommandShipJobService(
        IAgentsService agentsService,
        ISystemsService systemsService,
        IWaypointsService waypointsService)
    {
        _agentsService = agentsService;
        _systemsService = systemsService;
        _waypointsService = waypointsService;
    }

    public async Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var agent = await _agentsService.GetAsync();
        if (agent.Credits > 800_000)
        {
            var shipTypesInSystem = ships
                .Where(s => s.Nav.SystemSymbol == ship.Nav.SystemSymbol)
                .GroupBy(s => s.Registration.Role);
            var miningDrones = shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.EXCAVATOR.ToString())?.Count() ?? 0;
            var lightHaulers = shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.HAULER.ToString())?.Count() ?? 0;
            var surveyShips = shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.SURVEYOR.ToString())?.Count() ?? 0;
            if (miningDrones < 5
                || lightHaulers < 5
                || miningDrones == 0)
            {
                return new ShipCommand(ship.Symbol, ShipCommandEnum.PurchaseShip);
            }
        }
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        var unchartedWaypoints =
            system.Waypoints.Where(w =>
                w.Traits is null
                || w.Traits.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()))
            .Select(w => w.Symbol)
            .ToList();
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        if (paths.Keys.Any(p => unchartedWaypoints.Contains(p.Symbol)))
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration);
        }
        var jumpGate = system.Waypoints.SingleOrDefault(w =>
            w.Type == WaypointTypesEnum.JUMP_GATE.ToString()
            && w.JumpGate is not null
            && !w.IsUnderConstruction);
        if (jumpGate is not null)
        {
            var jumpSystems = jumpGate.JumpGate.Connections;
            foreach (var jumpSystem in jumpSystems)
            {
                var cacheSystem = await _systemsService.GetAsync(jumpSystem);
                if (cacheSystem.Waypoints.Any(w => w.Traits is null || !w.Traits.Any()))
                {
                    return new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration);
                }
            }
        }
        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }
}