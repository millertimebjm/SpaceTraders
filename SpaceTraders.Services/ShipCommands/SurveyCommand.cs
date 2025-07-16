using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class SurveyCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsService _waypointsService;
    private readonly ISystemsService _systemsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly IShipJobsFactory _shipJobsFactory;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.Survey;
    private readonly IAgentsService _agentsService;
    public SurveyCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IShipsService shipsService,
        IWaypointsService waypointsService,
        ISystemsService systemsService,
        IShipStatusesCacheService shipStatusesCacheService,
        IShipJobsFactory shipJobsFactory,
        IAgentsService agentsService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _shipsService = shipsService;
        _waypointsService = waypointsService;
        _systemsService = systemsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _shipJobsFactory = shipJobsFactory;
        _agentsService = agentsService;
    }

    public async Task<Ship> Run(
        Ship ship,
        Dictionary<string, Ship> shipsDictionary)
    {
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return ship;
            var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);

            var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

            await Task.Delay(2000);

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForFuel(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
                continue;
            }
            
            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            (nav, var fuel) = await _shipCommandsHelperService.NavigateToSurvey(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"NavigateToSurvey {nav.WaypointSymbol}", DateTime.UtcNow));
                return ship;
            }

            var cooldown = await _shipCommandsHelperService.Survey(ship);
            ship = ship with { Cooldown = cooldown };
            await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"Survey {ship.Nav.WaypointSymbol}", DateTime.UtcNow));
            return ship;
        }
    }
}