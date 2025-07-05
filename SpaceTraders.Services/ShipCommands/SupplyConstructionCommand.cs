using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class SupplyConstructionCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsService _waypointsService;
    private readonly ISystemsService _systemsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.SupplyConstruction;
    public SupplyConstructionCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IShipsService shipsService,
        IWaypointsService waypointsService,
        ISystemsService systemsService,
        IShipStatusesCacheService shipStatusesCacheService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _shipsService = shipsService;
        _waypointsService = waypointsService;
        _systemsService = systemsService;
        _shipStatusesCacheService = shipStatusesCacheService;
    }

    public async Task<Ship> Run(
        Ship ship,
        Waypoint constructionWaypoint)
    {
        //var ship = await _shipsService.GetAsync(shipSymbol);
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return ship;
            var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
            var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

            await Task.Delay(2000);

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
                continue;
            }

            var supplyResult = await _shipCommandsHelperService.SupplyConstructionSite(ship, currentWaypoint);
            if (supplyResult is not null)
            {
                ship = ship with { Cargo = supplyResult.Cargo };
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

            (nav, fuel) = await _shipCommandsHelperService.NavigateToConstructionWaypoint(ship, currentWaypoint, constructionWaypoint);
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
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"NavigateToMarketplaceExport", DateTime.UtcNow));
                return ship;
            }

            throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");

        }
    }
}