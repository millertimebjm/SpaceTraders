using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsAsyncRefreshService : ISystemsAsyncRefreshService
{
    private readonly IWaypointsApiService _waypointsApiService;
    private readonly ILogger<SystemsAsyncRefreshService> _logger;
    private readonly ISystemsCacheService _systemsCacheService;

    public SystemsAsyncRefreshService(
        IWaypointsApiService waypointsApiService,
        ILogger<SystemsAsyncRefreshService> logger,
        ISystemsCacheService systemsCacheService)
    {
        _waypointsApiService = waypointsApiService;
        _logger = logger;
        _systemsCacheService = systemsCacheService;
    }

    public async Task RefreshWaypointsAsync(STSystem system)
    {
        List<Waypoint> waypointsHydrated = new();
        foreach (var waypointSkeleton in system.Waypoints)
        {
            var completed = false;
            while (!completed)
            {
                try
                {
                    var waypointHydrated = await _waypointsApiService.GetAsync(waypointSkeleton.Symbol);
                    waypointsHydrated.Add(waypointHydrated);
                    completed = true;
                }
                catch (HttpRequestException)
                {
                    _logger.LogError("Waypoint/Shipyard/Marketplace Rate Limit error in {type} RefreshWaypointsAsync", nameof(SystemsAsyncRefreshService));
                    await Task.Delay(5000);
                }
            }

            // Wait one second for 429-Rate Limit issues
            await Task.Delay(1000);
        }
        system = system with { Waypoints = waypointsHydrated };
        await _systemsCacheService.SetAsync(system);
    }
}