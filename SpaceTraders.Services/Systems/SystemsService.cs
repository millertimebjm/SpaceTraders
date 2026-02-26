using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;

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

    public static IEnumerable<STSystem> Traverse(IEnumerable<STSystem> systems, string startingSystemString)
    {
        var traversableSystems = new List<STSystem>();
        var startingSystem = systems.Single(s => s.Symbol == startingSystemString);
        var systemsToTraverse = new Queue<STSystem>();
        systemsToTraverse.Enqueue(startingSystem);
        
        while (systemsToTraverse.Count != 0)
        {
            var nextSystem = systemsToTraverse.Dequeue();
            traversableSystems.Add(nextSystem);
            var jumpGateWaypoints = nextSystem.Waypoints.Where(w => !w.IsUnderConstruction && w.JumpGate is not null);
            foreach (var jumpGateWaypoint in jumpGateWaypoints)
            {
                foreach (var connection in jumpGateWaypoint.JumpGate.Connections)
                {
                    var connectionSystem = systems.SingleOrDefault(s => s.Symbol == WaypointsService.ExtractSystemFromWaypoint(connection));
                    var connectionSystemJumpGates = connectionSystem?.Waypoints.Where(w => !w.IsUnderConstruction && w.JumpGate is not null).ToList() ?? [];
                    
                    foreach (var connectionSystemWaypoint in connectionSystem?.Waypoints.Where(w => !w.IsUnderConstruction && w.JumpGate is not null).ToList() ?? [])
                    {
                        if (connectionSystemWaypoint.JumpGate.Connections.Select(c => WaypointsService.ExtractSystemFromWaypoint(c)).Contains(nextSystem.Symbol)
                            && !traversableSystems.Any(ts => ts.Symbol == connectionSystem.Symbol)
                            && !systemsToTraverse.Any(stt => stt.Symbol == connectionSystem.Symbol))
                        {
                            systemsToTraverse.Enqueue(connectionSystem);
                        }
                    }
                }
            }
        }
        return traversableSystems;
    }
}