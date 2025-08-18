using SpaceTraders.Models;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Paths;

public class PathsService : IPathsService
{
    private readonly ISystemsService _systemsService;
    public PathsService(
        ISystemsService systemsService
    )
    {
        _systemsService = systemsService;
    }

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

    public static Dictionary<Waypoint, (List<Waypoint>, bool, int, int)> BuildWaypointPath(
        IEnumerable<Waypoint> waypoints,
        Waypoint origin,
        int fuelMax,
        int fuelCurrent)
    {
        var currentWaypoint = origin;
        var currentFuel = fuelMax;
        // Waypoint, Path, TotalFuel, RemainingFuel
        Dictionary<Waypoint, (List<Waypoint>, bool, int, int)> bestPath = new()
        {
            { origin, ([origin], false, 0, fuelCurrent) }
        };
        while (bestPath.Values.Any(bp => !bp.Item2))
        {
            // find all waypoints that haven't been searched
            var waypointsToSearch = bestPath.Where(p => !p.Value.Item2);

            // from those waypoints that haven't been searched, find the least paths and then minimum fuel
            var waypointToSearch = waypointsToSearch.Where(w => w.Value.Item1.Count == waypointsToSearch.Min(wts => wts.Value.Item1.Count())).OrderBy(wts => wts.Value.Item3).FirstOrDefault();
            currentFuel = waypointToSearch.Value.Item4;
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
                    bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, fuelMax);
                }
                else
                {
                    bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, currentFuel - tempFuelUsage);
                }
            }
            bestPath[waypointToSearch.Key] = (waypointToSearch.Value.Item1, true, waypointToSearch.Value.Item3, waypointToSearch.Value.Item4);
        }
        return bestPath;
    }

    public async Task<Dictionary<Waypoint, (List<Waypoint>, int)>> BuildSystemPath(
        string originWaypoint,
        int fuelMax,
        int startingFuel)
    {
        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var currentWaypoint = waypoints.Single(w => w.Symbol == originWaypoint);

        var currentFuel = startingFuel;
        // Waypoint, Path, TotalFuel, RemainingFuel
        Dictionary<Waypoint, (List<Waypoint>, bool, int, int)> bestPath = new()
        {
            { currentWaypoint, ([currentWaypoint], false, 0, startingFuel) }
        };
        while (bestPath.Values.Any(bp => !bp.Item2))
        {
            // find all waypoints that haven't been searched
            var waypointsToSearch = bestPath.Where(p => !p.Value.Item2).ToList();

            // from those waypoints that haven't been searched, find the least paths and then minimum fuel
            var waypointToSearch = waypointsToSearch
                .Where(w => w.Value.Item1.Count == waypointsToSearch.Min(wts => wts.Value.Item1.Count()))
                .OrderBy(wts => wts.Value.Item3)
                .FirstOrDefault();
            currentFuel = waypointToSearch.Value.Item4;
            var currentPath = waypointToSearch.Value.Item1;

            var waypointsWithinRange = GetWaypointsWithinRange(waypoints, waypointToSearch.Key, currentFuel);
            //var waypointsWithinRange = waypoints.Where(w => WaypointsService.CalculateDistance(waypointToSearch.Key.X, waypointToSearch.Key.Y, w.X, w.Y) <= currentFuel);
            var waypointsWithinRangeNotReviewed = waypointsWithinRange.Where(wwr => !bestPath.Keys.Select(bp => bp.Symbol).Contains(wwr.Symbol)).ToList();
            foreach (var waypoint in waypointsWithinRangeNotReviewed)
            {
                var lastWaypoint = currentPath.Last();
                var tempPath = currentPath.ToList();
                tempPath.Add(waypoint);
                if (WaypointsService.ExtractSystemFromWaypoint(lastWaypoint.Symbol) == WaypointsService.ExtractSystemFromWaypoint(waypoint.Symbol))
                {
                    var tempFuelUsage = (int)Math.Ceiling(WaypointsService.CalculateDistance(lastWaypoint.X, lastWaypoint.Y, waypoint.X, waypoint.Y));
                    if (waypoint.Marketplace?.Exchange.ToList().Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
                    {
                        bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, fuelMax);
                    }
                    else
                    {
                        bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, currentFuel - tempFuelUsage);
                    }
                }
                else
                {
                    bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3, fuelMax);
                }
            }
            bestPath[waypointToSearch.Key] = (waypointToSearch.Value.Item1, true, waypointToSearch.Value.Item3, waypointToSearch.Value.Item4);
        }
        return bestPath.ToDictionary(p => p.Key, p => (p.Value.Item1, p.Value.Item3));
    }

    private static IReadOnlyList<Waypoint> GetWaypointsWithinRange(
        IEnumerable<Waypoint> waypoints,
        Waypoint currentWaypoint,
        int currentFuel)
    {
        var waypointsWithinRange = waypoints
            .Where(w =>
                WaypointsService.ExtractSystemFromWaypoint(w.Symbol) == WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol)
                && WaypointsService.CalculateDistance(currentWaypoint.X, currentWaypoint.Y, w.X, w.Y) <= currentFuel)
            .ToList();
        if (currentWaypoint.JumpGate is not null
            && !currentWaypoint.IsUnderConstruction)
        {
            var jumpGateWaypointSymbols = currentWaypoint.JumpGate.Connections;
            var jumpGateWaypoints = waypoints
                .Where(w => jumpGateWaypointSymbols.Contains(w.Symbol))
                .ToList();
            waypointsWithinRange.AddRange(jumpGateWaypoints);
        }
        return waypointsWithinRange;
    }

    public const int JUMP_COST = 1000;
    public async Task<Dictionary<Waypoint, (List<Waypoint>, int)>> BuildSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int startingFuel)
    {
        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var currentWaypoint = waypoints.Single(w => w.Symbol == originWaypoint);

        var currentFuel = startingFuel;
        // Waypoint, (Path, IsSearched, TotalFuel, RemainingFuel, Cost)
        Dictionary<Waypoint, (List<Waypoint>, bool, int, int, int)> bestPath = new()
        {
            { currentWaypoint, ([currentWaypoint], false, 0, startingFuel, 0) }
        };
        while (bestPath.Values.Any(bp => !bp.Item2))
        {
            // find all waypoints that haven't been searched
            var waypointsToSearch = bestPath.Where(p => !p.Value.Item2).ToList();

            // from those waypoints that haven't been searched, find the least paths and then minimum fuel
            var waypointToSearch = waypointsToSearch
                .Where(w => w.Value.Item1.Count == waypointsToSearch.Min(wts => wts.Value.Item1.Count()))
                .OrderBy(wts => wts.Value.Item4)
                .FirstOrDefault();
            currentFuel = waypointToSearch.Value.Item4;
            var currentPath = waypointToSearch.Value.Item1;

            var waypointSymbolsSearched = bestPath
                .Where(bp => bp.Value.Item2)
                .Select(bp => bp.Key.Symbol)
                .ToList();
            var destinationsToSearch = waypoints
                .Where(w => w.Symbol != waypointToSearch.Key.Symbol && !waypointSymbolsSearched.Contains(w.Symbol))
                .ToList();
            foreach (var waypoint in destinationsToSearch)
            {
                var lastWaypoint = currentPath.Last();
                var tempPath = currentPath.ToList();
                tempPath.Add(waypoint);
                if (WaypointsService.ExtractSystemFromWaypoint(lastWaypoint.Symbol) == WaypointsService.ExtractSystemFromWaypoint(waypoint.Symbol))
                {
                    var tempFuelUsage = (int)Math.Ceiling(WaypointsService.CalculateDistance(lastWaypoint.X, lastWaypoint.Y, waypoint.X, waypoint.Y));
                    int cost = tempFuelUsage;
                    if (tempFuelUsage >= fuelMax)
                    {
                        cost = tempFuelUsage * tempFuelUsage;
                        tempFuelUsage = 1;
                    }
                    if (waypoint.Marketplace?.Exchange.ToList().Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
                    {
                        bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, fuelMax, waypointToSearch.Value.Item5 + cost);
                    }
                    else
                    {
                        bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, currentFuel - tempFuelUsage, waypointToSearch.Value.Item5 + cost);
                    }
                }
                else
                {
                    bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3, fuelMax, waypointToSearch.Value.Item5 + JUMP_COST);
                }
            }
            bestPath[waypointToSearch.Key] = (waypointToSearch.Value.Item1, true, waypointToSearch.Value.Item3, waypointToSearch.Value.Item4, waypointToSearch.Value.Item5);
        }
        return bestPath.ToDictionary(p => p.Key, p => (p.Value.Item1, p.Value.Item5));
    }
}