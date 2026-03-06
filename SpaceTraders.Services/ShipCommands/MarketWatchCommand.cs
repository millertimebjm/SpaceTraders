using SpaceTraders.Models;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class MarketWatchCommand(
    IWaypointsService _waypointsService
) : IShipCommandsService
{
    private const int MARKET_WATCH_MINUTES = 15;
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var localShips = shipsDictionary.Values.Where(s => s.Nav.WaypointSymbol == ship.Nav.WaypointSymbol).ToList();
        if (localShips.Count > 1)
        {
            var destinationWaypoint = await FindEmptyMarketplace();
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

    private async Task<string> FindEmptyMarketplace()
    {
        throw new NotImplementedException();
    }
}