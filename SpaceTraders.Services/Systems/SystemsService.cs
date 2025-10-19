using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsService(
    ISystemsApiService _systemsApiService,
    ISystemsCacheService _systemsCacheService,
    ILogger<SystemsService> _logger
) : ISystemsService
{
    public async Task<IReadOnlyList<STSystem>> GetAsync()
    {
        return await _systemsCacheService.GetAsync();
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