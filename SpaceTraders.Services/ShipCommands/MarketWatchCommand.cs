using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class MarketWatchCommand(
    IWaypointsService _waypointsService,
    ISystemsService _systemsService,
    IShipsService _shipsService
) : IShipCommandsService
{
    private const int MARKET_WATCH_MINUTES = 15;
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var localShips = shipsDictionary.Values.Where(s => s.Nav.WaypointSymbol == ship.Nav.WaypointSymbol).ToList();
        var waypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if (localShips.Count > 1 || waypoint.Marketplace is null)
        {
            var destinationWaypoint = await FindEmptyMarketplace(ship, shipsDictionary);
            if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
            {
                await _shipsService.OrbitAsync(ship.Symbol);
            }
            var (nav, fuel) = await _shipsService.NavigateAsync(destinationWaypoint, ship);
            ship = ship with { Nav = nav, Fuel = fuel };
            shipStatus = shipStatus with { Ship = ship };
            return shipStatus;
        }
        if (ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString())
        {
            await _shipsService.DockAsync(ship.Symbol);
        }
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if (currentWaypoint.RefreshDateTimeUtc is null || currentWaypoint.RefreshDateTimeUtc < DateTime.UtcNow.AddMinutes(-15))
        {
            await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
        }
        var timespan = TimeSpan.FromMinutes(MARKET_WATCH_MINUTES);
        ship = ship with { Cooldown = new Cooldown(ship.Symbol, (int)timespan.TotalSeconds, (int)timespan.TotalSeconds, DateTime.UtcNow.AddMinutes(MARKET_WATCH_MINUTES)) };
        shipStatus = shipStatus with { Ship = ship };
        return shipStatus;
    }

    private async Task<string> FindEmptyMarketplace(Ship ship, Dictionary<string, Ship> shipsDictionary)
    {
        var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(ship.Nav.WaypointSymbol));
        var waypoints = system.Waypoints;
        var marketplaceWaypoints = waypoints.Where(w => w.Marketplace is not null).Select(w => w.Symbol).ToList();
        var shipWaypoints = shipsDictionary
            .Values
            .Where(sd => sd.Registration.Role == ShipRegistrationRolesEnum.SATELLITE.ToString())
            .Select(sd => sd.Nav.WaypointSymbol)
            .Distinct()
            .ToList();
        var emptyMarketplaceWaypoint = marketplaceWaypoints.Except(shipWaypoints).Except([ship.Nav.WaypointSymbol]).OrderBy(w => w).First();
        return emptyMarketplaceWaypoint;
    }
}