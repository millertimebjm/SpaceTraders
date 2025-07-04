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

public class BuyAndSellCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsService _waypointsService;
    private readonly ISystemsService _systemsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.BuyToSell;
    public BuyAndSellCommand(
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

    public async Task<DateTime?> Run(
        string shipSymbol,
        Waypoint miningWaypoint,
        Waypoint? sellingWaypoint)
    {
        while (true)
        {
            var ship = await _shipsService.GetAsync(shipSymbol);
            if (ShipsService.GetShipCooldown(ship) is not null) return DateTime.UtcNow + ShipsService.GetShipCooldown(ship);
            var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
            var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
            var inventorySymbols = ship.Cargo.Inventory.Select(i => i.Symbol).ToHashSet();

            var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

            sellingWaypoint = paths.Select(p => p.Key)
                .Where(w => w.Marketplace is not null
                    && w.Marketplace.Imports.Count(i => inventorySymbols.Contains(i.Symbol)) > 0)
                .OrderByDescending(w =>
                    w.Marketplace.Imports.Count(i => inventorySymbols.Contains(i.Symbol)))
                .FirstOrDefault();

            await Task.Delay(2000);

            var executed = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (executed) continue;

            executed = await _shipCommandsHelperService.Dock(ship, currentWaypoint);
            if (executed) continue;

            executed = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
            if (executed) continue;

            executed = await _shipCommandsHelperService.Buy(ship, currentWaypoint);
            if (executed) continue;

            executed = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (executed) continue;

            var delay = await _shipCommandsHelperService.NavigateToStartWaypoint(ship, currentWaypoint, miningWaypoint);
            if (delay is not null)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship.Symbol, _shipCommandEnum, ship.Cargo, $"NavigateToStartWaypoint {miningWaypoint.Symbol}"));
                return delay;
            }

            delay = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint, sellingWaypoint);
            if (delay is not null)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship.Symbol, _shipCommandEnum, ship.Cargo, $"NavigateToMarketplaceImport {sellingWaypoint.Symbol}"));
                return delay;
            }

            throw new Exception("Infinite loop, no work planned.");
        }
    }
}