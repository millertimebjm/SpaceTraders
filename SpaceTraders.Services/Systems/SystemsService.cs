using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsService : ISystemsService
{
    private readonly ISystemsApiService _systemsApiService;
    private readonly ISystemsCacheService _systemsCacheService;
    private readonly IWaypointsService _waypointsService;
    private readonly ILogger<SystemsService> _logger;

    public SystemsService(
        ISystemsApiService systemsApiService,
        ISystemsCacheService systemsCacheService,
        IWaypointsService waypointsService,
        ILogger<SystemsService> logger)
    {
        _systemsApiService = systemsApiService;
        _systemsCacheService = systemsCacheService;
        _waypointsService = waypointsService;
        _logger = logger;
    }

    public async Task<STSystem> GetAsync(string systemSymbol, bool refresh = false)
    {
        STSystem? system;
        if (!refresh)
        {
            system = await _systemsCacheService.GetAsync(systemSymbol, refresh);
            if (system is not null) return system;
            _logger.LogWarning("Cache miss: {type}: {id}", nameof(STSystem), systemSymbol);
        }

        system = await _systemsApiService.GetAsync(systemSymbol);
        await _systemsCacheService.SetAsync(system);
        return system;
    }
}