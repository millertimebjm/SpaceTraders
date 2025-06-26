using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Paths;

public static class PathsService// : IPathsService
{
    // private readonly ILogger<PathsService> _logger;
    // private readonly HttpClient _httpClient;
    // private readonly IConfiguration _configuration;

    // public PathsService(
    //     ILogger<PathsService> logger,
    //     HttpClient httpClient,
    //     IConfiguration configuration)
    // {
    //     _logger = logger;
    //     _httpClient = httpClient;
    //     _configuration = configuration;
    // }

    public static HashSet<Waypoint>? GetPathAsync(
        IEnumerable<Waypoint> waypoints,
        Waypoint origin,
        Waypoint destination,
        int fuelMax)
    {
        if (waypoints == null || !waypoints.Any())
        {
            throw new ArgumentException("Waypoints collection cannot be null or empty.", nameof(waypoints));
        }
        if (origin == null)
        {
            throw new ArgumentNullException(nameof(origin), "Origin waypoint cannot be null.");
        }
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination), "Destination waypoint cannot be null.");
        }
        if (fuelMax <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fuelMax), "Fuel capacity must be greater than zero.");
        }

        var shortestPath = FindShortestPath(
            WaypointsService.SortWaypoints(waypoints.ToList(), origin.X, origin.Y),
            origin,
            destination,
            fuelMax,
            (0, new HashSet<Waypoint>() { origin }),
            fuelMax,
            0);

        return shortestPath?.Item2;
    }

    internal static (int, HashSet<Waypoint>)? FindShortestPath(
        IOrderedEnumerable<Waypoint> waypoints,
        Waypoint currentWaypoint,
        Waypoint destination,
        int fuelMax,
        (int, HashSet<Waypoint>) visitedWithFuel,
        int currentFuel,
        int usedFuel)
    {
        if (currentWaypoint.Symbol == destination.Symbol) return visitedWithFuel;
        if (currentWaypoint.Type == WaypointTypesEnum.FUEL_STATION.ToString()) currentFuel = fuelMax;

        var allSolutions = new List<(int, HashSet<Waypoint>)>();
        foreach (var waypoint in waypoints)
        {
            if (currentWaypoint.Symbol == waypoint.Symbol) continue;
            if (!visitedWithFuel.Item2.Any(v => v.Symbol == waypoint.Symbol))
            {
                var requiredFuel = (int)Math.Ceiling(
                    WaypointsService.CalculateDistance(
                        currentWaypoint.X,
                        currentWaypoint.Y,
                        waypoint.X,
                        waypoint.Y));
                if (requiredFuel <= currentFuel)
                {
                    var newVisitedWithFuel = (visitedWithFuel.Item1 + requiredFuel, visitedWithFuel.Item2.ToHashSet());
                    newVisitedWithFuel.Item2.Add(waypoint);
                    var solution = FindShortestPath(waypoints,
                        waypoint,
                        destination,
                        fuelMax,
                        newVisitedWithFuel,
                        currentFuel - requiredFuel,
                        usedFuel + requiredFuel);
                    if (solution.HasValue)
                    {
                        allSolutions.Add(solution.Value);
                    }
                }
            }
        }

        return allSolutions.OrderBy(s => s.Item1).FirstOrDefault();
    }
}