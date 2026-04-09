using SpaceTraders.Models;
using SpaceTraders.Services.SystemRefresh.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.SystemRefresh;

public class SystemRefreshService(
    ISystemsService _systemsService,
    IWaypointsApiService _waypointsApiService,
    IWaypointsCacheService _waypointsCacheService
) : ISystemRefreshService
{
    public async Task<STSystem> RefreshSystem(string systemSymbol)
    {
        var system = await _systemsService.GetAsync(systemSymbol);
        foreach (var waypoint in system.Waypoints.ToList())
        {
            var apiWaypoint = await _waypointsApiService.GetAsync(waypoint.Symbol);
            await _waypointsCacheService.SetAsync(apiWaypoint, updateTradeModels: false);
        }
        system = await _systemsService.GetAsync(systemSymbol);
        foreach (var waypoint in system.Waypoints.ToList())
        {
            await _waypointsCacheService.SetAsync(waypoint);
        }
        return system;
    }
}