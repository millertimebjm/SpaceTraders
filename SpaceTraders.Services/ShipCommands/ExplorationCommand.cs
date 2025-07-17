using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class ExplorationCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IWaypointsService _waypointsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly ITransactionsService _transactionsService;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.PurchaseShip;
    public ExplorationCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IWaypointsService waypointsService,
        IShipStatusesCacheService shipStatusesCacheService,
        ITransactionsService transactionsService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _waypointsService = waypointsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _transactionsService = transactionsService;
    }

    public async Task<Ship> Run(
        Ship ship,
        Dictionary<string, Ship> shipsDictionary)
    {
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if (currentWaypoint.Traits is null
            || !currentWaypoint.Traits.Any()
            || currentWaypoint.Traits.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()))
        {
            currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
        }

        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return ship;

            await Task.Delay(2000);

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _transactionsService.SetAsync(refuelResponse.Transaction);
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForFuel(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);

                continue;
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            (nav, var fuel) = await _shipCommandsHelperService.NavigateToExplore(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"NavigateToShipyardWaypoint {ship.Nav.WaypointSymbol}", DateTime.UtcNow));
                return ship;
            }

            ship = ship with { ShipCommand = null };
            await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"No instructions set.", DateTime.UtcNow));
            return ship;
        }
    }
}