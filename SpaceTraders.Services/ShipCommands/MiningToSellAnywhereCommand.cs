using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class MiningToSellAnywhereCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsService _waypointsService;
    private readonly ISystemsService _systemsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.MiningToSellAnywhere;
    public MiningToSellAnywhereCommand(
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
        Waypoint miningWaypoint)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return DateTime.UtcNow + ShipsService.GetShipCooldown(ship);
            var sellingWaypoint = await _shipCommandsHelperService.GetClosestSellingWaypoint(ship, currentWaypoint);

            await Task.Delay(2000);

            var executed = await _shipCommandsHelperService.Jettison(ship);
            if (executed) continue;

            var fuel = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (fuel is not null)
            {
                ship = ship with { Fuel = fuel };
                continue;
            }

            var nav = await _shipCommandsHelperService.Dock(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            var cargo = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
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

            var delay = await _shipCommandsHelperService.NavigateToStartWaypoint(ship, currentWaypoint, miningWaypoint);
            if (delay is not null)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship.Symbol, _shipCommandEnum, ship.Cargo, $"NavigateToStartWaypoint {miningWaypoint.Symbol}", DateTime.UtcNow));
                return delay;
            }

            delay = await _shipCommandsHelperService.Extract(ship, currentWaypoint, miningWaypoint);
            if (delay is not null)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship.Symbol, _shipCommandEnum, ship.Cargo, $"Extract {miningWaypoint.Symbol}", DateTime.UtcNow));
                return delay;
            }

            delay = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint, sellingWaypoint);
            if (delay is not null)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship.Symbol, _shipCommandEnum, ship.Cargo, $"NavigateToMarketplaceImport {sellingWaypoint?.Symbol}", DateTime.UtcNow));
                return delay;
            }

            throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
        }
    }
}