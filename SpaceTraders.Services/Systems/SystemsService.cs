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

    public async Task<List<STSystem>> GetAsync(List<string> systemSymbols)
    {
        return await _systemsCacheService.GetAsync(systemSymbols);
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

    public static IEnumerable<STSystem> Traverse(IEnumerable<STSystem> systems, string startingSystemString, int maxDepth = 5)
    {
        var traversableSystems = new List<STSystem>();
        var startingSystem = systems.Single(s => s.Symbol == startingSystemString);
        
        // Use a Queue of tuples to track (System, Depth)
        var systemsToTraverse = new Queue<(STSystem System, int Depth)>();
        systemsToTraverse.Enqueue((startingSystem, 0));
        
        // Use a HashSet for O(1) lookups to avoid duplicates/infinite loops
        var visitedSymbols = new HashSet<string> { startingSystem.Symbol };

        while (systemsToTraverse.Count != 0)
        {
            var (nextSystem, currentDepth) = systemsToTraverse.Dequeue();
            traversableSystems.Add(nextSystem);

            // If we've reached the max depth, don't enqueue its neighbors
            if (currentDepth >= maxDepth) continue;

            var jumpGateWaypoints = nextSystem.Waypoints.Where(w => !w.IsUnderConstruction && w.JumpGate is not null);
            
            foreach (var jumpGateWaypoint in jumpGateWaypoints)
            {
                foreach (var connection in jumpGateWaypoint.JumpGate!.Connections)
                {
                    var connectionSymbol = WaypointsService.ExtractSystemFromWaypoint(connection);
                    var connectionSystem = systems.SingleOrDefault(s => s.Symbol == connectionSymbol);
                    
                    if (connectionSystem is null || visitedSymbols.Contains(connectionSystem.Symbol)) 
                        continue;

                    // Check if the connection has a path back or is a valid jump gate
                    var hasValidGate = connectionSystem.Waypoints.Any(w => 
                        !w.IsUnderConstruction && 
                        w.JumpGate is not null &&
                        w.JumpGate.Connections.Any(c => WaypointsService.ExtractSystemFromWaypoint(c) == nextSystem.Symbol));

                    if (hasValidGate)
                    {
                        visitedSymbols.Add(connectionSystem.Symbol);
                        systemsToTraverse.Enqueue((connectionSystem, currentDepth + 1));
                    }
                }
            }
        }
        return traversableSystems;
    }

    public static List<(STSystem leftSystem, STSystem rightSystem, bool dottedLine)> TraverseLinks(List<STSystem> systems, string startingSystemString, bool traversable = false)
    {
        var startingSystem = systems.SingleOrDefault(s => s.Symbol == startingSystemString);
        List<(STSystem leftSystem, STSystem rightSystem, bool dottedLine)> links = [];
        foreach (var system in systems)
        {
            var jumpGateWaypoint = system.Waypoints.SingleOrDefault(w => w.JumpGate is not null);
            if (jumpGateWaypoint is null) continue;
            foreach (var connection in jumpGateWaypoint.JumpGate.Connections)
            {
                var connectedSystem = systems.SingleOrDefault(s => s.Symbol == WaypointsService.ExtractSystemFromWaypoint(connection));
                if (connectedSystem is null) continue;

                var dottedLine = jumpGateWaypoint.IsUnderConstruction || connectedSystem.Waypoints.Any(w => w.JumpGate is not null && w.IsUnderConstruction);
                if (!links.Any(l => l.leftSystem.Symbol == system.Symbol && l.rightSystem.Symbol == connectedSystem.Symbol)
                    && !links.Any(l => l.rightSystem.Symbol == system.Symbol && l.leftSystem.Symbol == connectedSystem.Symbol))
                {
                    if (!traversable || !dottedLine)
                    {
                        links.Add((system, connectedSystem, dottedLine));
                    }
                }
            }
        }

        return links;
    }

    public static List<(STSystem leftSystem, STSystem rightSystem, bool dottedLine, int distance)> TraverseLinksWithDistance(
        List<STSystem> systems, 
        string startingSystemString, 
        bool traversable = false)
    {
        var startingSystem = systems.Single(s => s.Symbol == startingSystemString);
        List<(STSystem leftSystem, STSystem rightSystem, bool dottedLine, int distance)> links = [];
        var systemsToReview = new List<(string SystemSymbol, int Distance)>() { (startingSystem.Symbol, 0) };
        while (systemsToReview.Any())
        {
            var systemToReview = systemsToReview.First();
            systemsToReview.Remove(systemToReview);

            var system = systems.Single(s => s.Symbol == systemToReview.SystemSymbol);
            var jumpGateWaypoint = system.Waypoints.Single(w => w.JumpGate is not null);
            foreach (var connection in jumpGateWaypoint!.JumpGate!.Connections)
            {
                var connectedSystem = systems.SingleOrDefault(s => s.Symbol == WaypointsService.ExtractSystemFromWaypoint(connection));
                if (connectedSystem is null) continue;

                if (links.Any(l => l.rightSystem.Symbol == system.Symbol && l.leftSystem.Symbol == connectedSystem.Symbol)) continue; 

                var dottedLine = jumpGateWaypoint.IsUnderConstruction || connectedSystem.Waypoints.Any(w => w.JumpGate is not null && w.IsUnderConstruction);
                links.Add((system, connectedSystem, dottedLine, systemToReview.Distance + 1));
            }
        }
        return links;
    }

    public static List<string> GetSystemSymbolsWithinXJumps(IReadOnlyList<STSystem> systems, string originSystemSymbol, int distance = 1, bool traversable = false)
    {
        var systemLinks = SystemsService.TraverseLinks(systems.ToList(), originSystemSymbol, traversable: traversable);
        List<string> currentLinks = [originSystemSymbol];

        for (int i = 0; i < distance; i++)
        {
            var newSystems = systemLinks.Where(sl => currentLinks.Contains(sl.leftSystem.Symbol) || currentLinks.Contains(sl.rightSystem.Symbol)).ToList();
            currentLinks.AddRange(newSystems.Select(sl => sl.leftSystem.Symbol));
            currentLinks.AddRange(newSystems.Select(sl => sl.rightSystem.Symbol));
            currentLinks = currentLinks.Distinct().ToList();
        }

        return currentLinks;
    }
}