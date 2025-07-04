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
        Waypoint miningWaypoint,
        Waypoint? sellingWaypoint)
    {
        while (true)
        {
            var ship = await _shipsService.GetAsync(shipSymbol);
            if (ShipsService.GetShipCooldown(ship) is not null) return DateTime.UtcNow + ShipsService.GetShipCooldown(ship);
            var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
            sellingWaypoint = await _shipCommandsHelperService.GetClosestSellingWaypoint(ship, currentWaypoint);

            await Task.Delay(2000);

            var executed = await _shipCommandsHelperService.Jettison(ship);
            if (executed) continue;

            executed = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (executed) continue;

            executed = await _shipCommandsHelperService.Dock(ship, currentWaypoint);
            if (executed) continue;

            executed = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
            if (executed) continue;

            executed = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (executed) continue;

            var delay = await _shipCommandsHelperService.NavigateToStartWaypoint(ship, currentWaypoint, miningWaypoint);
            if (delay is not null)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship.Symbol, _shipCommandEnum, ship.Cargo, $"NavigateToStartWaypoint {miningWaypoint.Symbol}"));
                return delay;
            }

            delay = await _shipCommandsHelperService.Extract(ship, currentWaypoint, miningWaypoint);
            if (delay is not null)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship.Symbol, _shipCommandEnum, ship.Cargo, $"Extract {miningWaypoint.Symbol}"));
                return delay;
            }

            delay = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint, sellingWaypoint);
            if (delay is not null)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship.Symbol, _shipCommandEnum, ship.Cargo, $"NavigateToMarketplaceImport {sellingWaypoint?.Symbol}"));
                return delay;
            }

            throw new Exception("Infinite loop, no work planned.");
        }
    }
}