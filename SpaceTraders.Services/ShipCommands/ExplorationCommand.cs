using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class ExplorationCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IWaypointsService _waypointsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly ITransactionsService _transactionsService;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsCacheService _waypointsCacheService;
    public ExplorationCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IWaypointsService waypointsService,
        IShipStatusesCacheService shipStatusesCacheService,
        ITransactionsService transactionsService,
        IShipsService shipsService,
        IWaypointsCacheService waypointsCacheService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _waypointsService = waypointsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _transactionsService = transactionsService;
        _shipsService = shipsService;
        _waypointsCacheService = waypointsCacheService;
    }

    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if (!WaypointsService.IsVisited(currentWaypoint))
        {
            if (currentWaypoint.Traits.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()))
            {
                try
                {
                    var chartWaypointResult = await _shipsService.ChartAsync(ship.Symbol);
                    currentWaypoint = chartWaypointResult.Waypoint;
                    await _waypointsCacheService.SetAsync(currentWaypoint);
                    // currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
                    // return shipStatus;
                }
                catch (Exception ex)
                {

                }
            }
            currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
        }

        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return shipStatus;

            await Task.Delay(1000);

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

            (nav, var fuel, var cooldown) = await _shipCommandsHelperService.NavigateToExplore(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
                return new ShipStatus(ship, $"NavigateToExplore {ship.Nav.WaypointSymbol}", DateTime.UtcNow);
            }

            ship = ship with { ShipCommand = null };
            return new ShipStatus(ship, $"No instructions set.", DateTime.UtcNow);
        }
    }
}