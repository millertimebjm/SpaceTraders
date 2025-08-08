using System.Security.Cryptography.X509Certificates;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class CommandShipJobService : IShipJobService
{
    private readonly IAgentsService _agentsService;
    private readonly ISystemsService _systemsService;
    private readonly IWaypointsService _waypointsService;
    private readonly IPathsService _pathsService;
    public CommandShipJobService(
        IAgentsService agentsService,
        ISystemsService systemsService,
        IWaypointsService waypointsService,
        IPathsService pathsService)
    {
        _agentsService = agentsService;
        _systemsService = systemsService;
        _waypointsService = waypointsService;
        _pathsService = pathsService;
    }

    public async Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        var unchartedWaypoints =
            waypoints
                .Where(w => !WaypointsService.IsVisited(w) || w.Traits?.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()) == true)
                .Select(w => w.Symbol)
                .ToList();
        if (unchartedWaypoints.Any())
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration);
        }
        // var paths = PathsService.BuildWaypointPath(waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
            // if (paths.Keys.Any(p => unchartedWaypoints.Contains(p.Symbol)))
            // {
            //     return new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration);
            // }

        var agent = await _agentsService.GetAsync();
        if (agent.Credits > 800_000)
        {
            var shipTypesInSystem = ships
                //.Where(s => s.Nav.SystemSymbol == ship.Nav.SystemSymbol)
                .GroupBy(s => s.Registration.Role);
            var miningDrones = shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.EXCAVATOR.ToString())?.Count() ?? 0;
            var lightHaulers = shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.HAULER.ToString())?.Count() ?? 0;
            var surveyShips = shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.SURVEYOR.ToString())?.Count() ?? 0;
            if ((miningDrones < 9
                || lightHaulers < 5)
                || surveyShips == 0)
            {
                return new ShipCommand(ship.Symbol, ShipCommandEnum.PurchaseShip);
            }
        }

        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }
}