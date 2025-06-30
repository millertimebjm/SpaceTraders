using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Paths;

public static class PathsService
{
    public static IEnumerable<Waypoint>? GetPath(
        Dictionary<Waypoint, (List<Waypoint>, bool, int, int)> waypoints,
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
            return null;
        }

        return waypoints[destination].Item1;
    }

    public static Dictionary<Waypoint, (List<Waypoint>, bool, int, int)> BuildDijkstraPath(
        IEnumerable<Waypoint> waypoints,
        Waypoint origin,
        int fuelMax)
    {
        var currentWaypoint = origin;
        var totalFuelUsage = 0;
        var currentFuel = fuelMax;
        // Waypoint, Path, TotalFuel, RemainingFuel
        Dictionary<Waypoint, (List<Waypoint>, bool, int, int)> bestPath = new()
        {
            { origin, ([origin], false, 0, 0) }
        };
        while (bestPath.Values.Any(bp => !bp.Item2))
        {
            // find all waypoints that haven't been searched
            var waypointsToSearch = bestPath.Where(p => !p.Value.Item2);

            // from those waypoints that haven't been searched, find the least paths and then minimum fuel
            var waypointToSearch = waypointsToSearch.Where(w => w.Value.Item1.Count == waypointsToSearch.Min(wts => wts.Value.Item1.Count())).OrderBy(wts => wts.Value.Item3).FirstOrDefault();
            var currentPath = waypointToSearch.Value.Item1;
            var waypointsWithinRange = waypoints.Where(w => WaypointsService.CalculateDistance(waypointToSearch.Key.X, waypointToSearch.Key.Y, w.X, w.Y) <= currentFuel);
            var waypointsWithinRangeNotReviewed = waypointsWithinRange.Where(wwr => !bestPath.Keys.Select(bp => bp.Symbol).Contains(wwr.Symbol));
            foreach (var waypoint in waypointsWithinRangeNotReviewed)
            {
                var lastWaypoint = currentPath.Last();
                var tempPath = currentPath.ToList();
                tempPath.Add(waypoint);
                var tempFuelUsage = (int)Math.Ceiling(WaypointsService.CalculateDistance(lastWaypoint.X, lastWaypoint.Y, waypoint.X, waypoint.Y));
                if (waypoint.Marketplace?.Exchange.ToList().Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
                {
                    bestPath[waypoint] = (tempPath, false, totalFuelUsage + tempFuelUsage, fuelMax);
                }
                else
                {
                    bestPath[waypoint] = (tempPath, false, totalFuelUsage + tempFuelUsage, currentFuel - tempFuelUsage);
                }
            }
            bestPath[waypointToSearch.Key] = (waypointToSearch.Value.Item1, true, waypointToSearch.Value.Item3, waypointToSearch.Value.Item4);
        }
        return bestPath;
    }
}