using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class PurchaseShipCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IWaypointsService _waypointsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.PurchaseShip;
    public PurchaseShipCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IWaypointsService waypointsService,
        IShipStatusesCacheService shipStatusesCacheService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _waypointsService = waypointsService;
        _shipStatusesCacheService = shipStatusesCacheService;
    }

    public async Task<Ship> Run(
        Ship ship,
        Dictionary<string, Ship> shipsDictionary)
    {
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        //var agent = _agentsService.GetAsync();

        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return ship;

            await Task.Delay(2000);

            var fuel = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (fuel is not null)
            {
                ship = ship with { Fuel = fuel };
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForShipyard(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);

                continue;
            }

            var executed = await _shipCommandsHelperService.PurchaseShip(ship, currentWaypoint);
            if (executed)
            {
                ship = ship with { ShipCommand = null };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, ship.ShipCommand?.ShipCommandEnum, ship.Cargo, $"NavigateToShipyardWaypoint {ship.Nav.WaypointSymbol}", DateTime.UtcNow));
                return ship;
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            (nav, fuel) = await _shipCommandsHelperService.NavigateToShipyard(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"NavigateToShipyardWaypoint {ship.Nav.WaypointSymbol}", DateTime.UtcNow));
                return ship;
            }

            throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
        }
    }
}