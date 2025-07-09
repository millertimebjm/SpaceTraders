using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class SupplyConstructionCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IWaypointsService _waypointsService;
    private readonly ISystemsService _systemsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly IShipJobsFactory _shipJobsFactory;
    private readonly IAgentsService _agentsService;
    private readonly IWaypointsCacheService _waypointsCacheService;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.SupplyConstruction;
    public SupplyConstructionCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IWaypointsService waypointsService,
        ISystemsService systemsService,
        IShipStatusesCacheService shipStatusesCacheService,
        IShipJobsFactory shipJobsFactory,
        IAgentsService agentsService,
        IWaypointsCacheService waypointsCacheService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _waypointsService = waypointsService;
        _systemsService = systemsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _shipJobsFactory = shipJobsFactory;
        _agentsService = agentsService;
        _waypointsCacheService = waypointsCacheService;
    }

    public async Task<Ship> Run(
        Ship ship,
        Dictionary<string, Ship> shipsDictionary)
    {
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var constructionWaypoint = system.Waypoints.FirstOrDefault(w => w.JumpGate is not null && w.IsUnderConstruction);
        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return ship;
            var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

            await Task.Delay(2000);

            // var cargo = await _shipCommandsHelperService.JettisonForSupplyConstruction(ship, constructionWaypoint);
            // if (cargo is not null)
            // {
            //     ship = ship with { Cargo = cargo };
            //     continue;
            // }

            var fuel = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (fuel is not null)
            {
                ship = ship with { Fuel = fuel };
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForSupplyConstruction(ship, currentWaypoint, constructionWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
                continue;
            }

            var supplyResult = await _shipCommandsHelperService.SupplyConstructionSite(ship, currentWaypoint);
            if (supplyResult is not null)
            {
                ship = ship with { Cargo = supplyResult.Cargo };
                currentWaypoint = currentWaypoint with { Construction = supplyResult.Construction };
                await _waypointsCacheService.SetAsync(currentWaypoint);

                if (!currentWaypoint.IsUnderConstruction)
                {
                    ship = ship with { ShipCommand = null };
                    await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, ship.ShipCommand?.ShipCommandEnum, ship.Cargo, $"Resetting Job.", DateTime.UtcNow));
                    return ship;
                }
            
                continue;
            }

            var cargo = await _shipCommandsHelperService.BuyForConstruction(ship, currentWaypoint, constructionWaypoint);
            if (cargo is not null)
            {
                ship = ship with { Cargo = cargo };
                continue;
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            (nav, fuel) = await _shipCommandsHelperService.NavigateToConstructionWaypoint(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"NavigateToStartWaypoint {constructionWaypoint.Symbol}", DateTime.UtcNow));
                return ship;
            }

            (nav, fuel) = await _shipCommandsHelperService.NavigateToMarketplaceExport(ship, currentWaypoint, constructionWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"NavigateToMarketplaceExport {nav.Route.Destination.Symbol}", DateTime.UtcNow));
                return ship;
            }

            throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");

        }
    }
}