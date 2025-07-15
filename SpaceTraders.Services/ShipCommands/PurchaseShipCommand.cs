using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
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
    private readonly IAgentsService _agentsService;
    public PurchaseShipCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IWaypointsService waypointsService,
        IShipStatusesCacheService shipStatusesCacheService,
        IAgentsService agentsService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _waypointsService = waypointsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _agentsService = agentsService;
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

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForShipyard(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);

                continue;
            }

            var purchaseShipResponse = await _shipCommandsHelperService.PurchaseShip(ship, currentWaypoint);
            if (purchaseShipResponse is not null)
            {
                ship = ship with { ShipCommand = null };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, ship.ShipCommand?.ShipCommandEnum, ship.Cargo, $"NavigateToShipyardWaypoint {ship.Nav.WaypointSymbol}", DateTime.UtcNow));
                await _shipStatusesCacheService.SetAsync(new ShipStatus(purchaseShipResponse.Ship, null, purchaseShipResponse.Ship.Cargo, $"Newly purchase ship.", DateTime.UtcNow));
                await _agentsService.SetAsync(purchaseShipResponse.Agent);
                return ship;
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            (nav, var fuel) = await _shipCommandsHelperService.NavigateToShipyard(ship, currentWaypoint);
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