using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Paths;

public class PathsService(
    ISystemsService _systemsService,
    IPathsCacheService _pathsCacheService
) : IPathsService
{
    // public static IEnumerable<Waypoint>? GetPath(
    //     Dictionary<Waypoint, ValueTuple<List<Waypoint>, bool, int, int>> waypoints,
    //     Waypoint origin,
    //     Waypoint destination,
    //     int fuelMax)
    // {
    //     if (waypoints == null || !waypoints.Any())
    //     {
    //         throw new ArgumentException("Waypoints collection cannot be null or empty.", nameof(waypoints));
    //     }
    //     if (origin == null)
    //     {
    //         throw new ArgumentNullException(nameof(origin), "Origin waypoint cannot be null.");
    //     }
    //     if (destination == null)
    //     {
    //         throw new ArgumentNullException(nameof(destination), "Destination waypoint cannot be null.");
    //     }
    //     if (fuelMax <= 0)
    //     {
    //         return null;
    //     }

    //     return waypoints[destination].Item1;
    // }

    // public static Dictionary<Waypoint, (List<Waypoint>, bool, int, int)> BuildWaypointPath(
    //     IEnumerable<Waypoint> waypoints,
    //     Waypoint origin,
    //     int fuelMax,
    //     int fuelCurrent)
    // {
    //     var currentWaypoint = origin;
    //     var currentFuel = fuelMax;
    //     // Waypoint, Path, TotalFuel, RemainingFuel
    //     Dictionary<Waypoint, (List<Waypoint>, bool, int, int)> bestPath = new()
    //     {
    //         { origin, ([origin], false, 0, fuelCurrent) }
    //     };
    //     while (bestPath.Values.Any(bp => !bp.Item2))
    //     {
    //         // find all waypoints that haven't been searched
    //         var waypointsToSearch = bestPath.Where(p => !p.Value.Item2);

    //         // from those waypoints that haven't been searched, find the least paths and then minimum fuel
    //         var waypointToSearch = waypointsToSearch.Where(w => w.Value.Item1.Count == waypointsToSearch.Min(wts => wts.Value.Item1.Count())).OrderBy(wts => wts.Value.Item3).FirstOrDefault();
    //         currentFuel = waypointToSearch.Value.Item4;
    //         var currentPath = waypointToSearch.Value.Item1;
    //         var waypointsWithinRange = waypoints.Where(w => WaypointsService.CalculateDistance(waypointToSearch.Key.X, waypointToSearch.Key.Y, w.X, w.Y) <= currentFuel);
    //         var waypointsWithinRangeNotReviewed = waypointsWithinRange.Where(wwr => !bestPath.Keys.Select(bp => bp.Symbol).Contains(wwr.Symbol));
    //         foreach (var waypoint in waypointsWithinRangeNotReviewed)
    //         {
    //             var lastWaypoint = currentPath.Last();
    //             var tempPath = currentPath.ToList();
    //             tempPath.Add(waypoint);
    //             var tempFuelUsage = (int)Math.Ceiling(WaypointsService.CalculateDistance(lastWaypoint.X, lastWaypoint.Y, waypoint.X, waypoint.Y));
    //             if (waypoint.Marketplace?.Exchange.ToList().Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
    //             {
    //                 bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, fuelMax);
    //             }
    //             else
    //             {
    //                 bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, currentFuel - tempFuelUsage);
    //             }
    //         }
    //         bestPath[waypointToSearch.Key] = (waypointToSearch.Value.Item1, true, waypointToSearch.Value.Item3, waypointToSearch.Value.Item4);
    //     }
    //     return bestPath;
    // }

    // public async Task<Dictionary<string, ValueTuple<List<string>, int>>> BuildSystemPath(
    //     string originWaypoint,
    //     int fuelMax,
    //     int startingFuel)
    // {
    //     var systems = await _systemsService.GetAsync();
    //     var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(originWaypoint));
    //     var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
    //     var currentWaypoint = waypoints.Single(w => w.Symbol == originWaypoint);

    //     var currentFuel = startingFuel;
    //     // Waypoint, Path, TotalFuel, RemainingFuel
    //     Dictionary<Waypoint, (List<Waypoint>, bool, int, int)> bestPath = new()
    //     {
    //         { currentWaypoint, ([currentWaypoint], false, 0, startingFuel) }
    //     };
    //     while (bestPath.Values.Any(bp => !bp.Item2))
    //     {
    //         // find all waypoints that haven't been searched
    //         var waypointsToSearch = bestPath.Where(p => !p.Value.Item2).ToList();

    //         // from those waypoints that haven't been searched, find the least paths and then minimum fuel
    //         var waypointToSearch = waypointsToSearch
    //             .Where(w => w.Value.Item1.Count == waypointsToSearch.Min(wts => wts.Value.Item1.Count()))
    //             .OrderBy(wts => wts.Value.Item3)
    //             .FirstOrDefault();
    //         currentFuel = waypointToSearch.Value.Item4;
    //         var currentPath = waypointToSearch.Value.Item1;

    //         var waypointsWithinRange = GetWaypointsWithinRange(waypoints, waypointToSearch.Key, currentFuel);
    //         //var waypointsWithinRange = waypoints.Where(w => WaypointsService.CalculateDistance(waypointToSearch.Key.X, waypointToSearch.Key.Y, w.X, w.Y) <= currentFuel);
    //         var waypointsWithinRangeNotReviewed = waypointsWithinRange.Where(wwr => !bestPath.Keys.Select(bp => bp.Symbol).Contains(wwr.Symbol)).ToList();
    //         foreach (var waypoint in waypointsWithinRangeNotReviewed)
    //         {
    //             var lastWaypoint = currentPath.Last();
    //             var tempPath = currentPath.ToList();
    //             tempPath.Add(waypoint);
    //             if (WaypointsService.ExtractSystemFromWaypoint(lastWaypoint.Symbol) == WaypointsService.ExtractSystemFromWaypoint(waypoint.Symbol))
    //             {
    //                 var tempFuelUsage = (int)Math.Ceiling(WaypointsService.CalculateDistance(lastWaypoint.X, lastWaypoint.Y, waypoint.X, waypoint.Y));
    //                 if (waypoint.Marketplace?.Exchange.ToList().Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
    //                 {
    //                     bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, fuelMax);
    //                 }
    //                 else
    //                 {
    //                     bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, currentFuel - tempFuelUsage);
    //                 }
    //             }
    //             else
    //             {
    //                 bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3, fuelMax);
    //             }
    //         }
    //         bestPath[waypointToSearch.Key] = (waypointToSearch.Value.Item1, true, waypointToSearch.Value.Item3, waypointToSearch.Value.Item4);
    //     }
    //     return bestPath.ToDictionary(p => p.Key.Symbol, p => (p.Value.Item1.Select(i => i.Symbol).ToList(), p.Value.Item3));
    // }

    // private static IReadOnlyList<Waypoint> GetWaypointsWithinRange(
    //     IEnumerable<Waypoint> waypoints,
    //     Waypoint currentWaypoint,
    //     int currentFuel)
    // {
    //     var waypointsWithinRange = waypoints
    //         .Where(w =>
    //             WaypointsService.ExtractSystemFromWaypoint(w.Symbol) == WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol)
    //             && WaypointsService.CalculateDistance(currentWaypoint.X, currentWaypoint.Y, w.X, w.Y) <= currentFuel)
    //         .ToList();
    //     if (currentWaypoint.JumpGate is not null
    //         && !currentWaypoint.IsUnderConstruction)
    //     {
    //         var jumpGateWaypointSymbols = currentWaypoint.JumpGate.Connections;
    //         var jumpGateWaypoints = waypoints
    //             .Where(w => jumpGateWaypointSymbols.Contains(w.Symbol))
    //             .ToList();
    //         waypointsWithinRange.AddRange(jumpGateWaypoints);
    //     }
    //     return waypointsWithinRange;
    // }

    // public const int JUMP_COST = 1000;

    // public async Task<Dictionary<string, ValueTuple<List<string>, int>>> BuildSystemPathWithCost(
    //     string originWaypoint,
    //     int fuelMax,
    //     int startingFuel)
    // {
    //     var systemPath = await _pathsCacheService.GetSystemPathWithCost(originWaypoint, fuelMax, startingFuel);
    //     if (systemPath is not null) return systemPath;

    //     var systems = await _systemsService.GetAsync();
    //     var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(originWaypoint));
    //     var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
    //     var currentWaypoint = waypoints.Single(w => w.Symbol == originWaypoint);

    //     systemPath = await BuildSystemPathWithCost(waypoints, currentWaypoint, fuelMax, startingFuel);
    //     await _pathsCacheService.SetSystemPathWithCost(currentWaypoint.Symbol, fuelMax, startingFuel, systemPath);
    //     return systemPath;
    // }
    
    // public async Task<Dictionary<string, ValueTuple<List<string>, int>>> BuildSystemPathWithCost(
    //     List<Waypoint> waypoints,
    //     Waypoint currentWaypoint,
    //     int fuelMax,
    //     int startingFuel)
    // {
    //     var systemPath = await _pathsCacheService.GetSystemPathWithCost(currentWaypoint.Symbol, fuelMax, startingFuel);
    //     if (systemPath is not null) return systemPath;

    //     var currentFuel = startingFuel;
    //     // Waypoint, (Path, IsSearched, TotalFuel, RemainingFuel, Cost)
    //     Dictionary<Waypoint, (List<Waypoint>, bool, int, int, int)> bestPath = new()
    //     {
    //         { currentWaypoint, ([currentWaypoint], false, 0, startingFuel, 0) }
    //     };
    //     while (bestPath.Values.Any(bp => !bp.Item2))
    //     {
    //         // find all waypoints that haven't been searched
    //         var waypointsToSearch = bestPath.Where(p => !p.Value.Item2).ToList();

    //         // from those waypoints that haven't been searched, find the least paths and then minimum fuel
    //         var waypointToSearch = waypointsToSearch
    //             .Where(w => w.Value.Item1.Count == waypointsToSearch.Min(wts => wts.Value.Item1.Count()))
    //             .OrderBy(wts => wts.Value.Item4)
    //             .FirstOrDefault();
    //         currentFuel = waypointToSearch.Value.Item4;
    //         var currentPath = waypointToSearch.Value.Item1;

    //         var waypointSymbolsSearched = bestPath
    //             .Where(bp => bp.Value.Item2)
    //             .Select(bp => bp.Key.Symbol)
    //             .ToList();
    //         var destinationsToSearch = waypoints
    //             .Where(w => w.Symbol != waypointToSearch.Key.Symbol && !waypointSymbolsSearched.Contains(w.Symbol))
    //             .ToList();
    //         foreach (var waypoint in destinationsToSearch)
    //         {
    //             var lastWaypoint = currentPath.Last();
    //             var tempPath = currentPath.ToList();
    //             tempPath.Add(waypoint);
    //             if (WaypointsService.ExtractSystemFromWaypoint(lastWaypoint.Symbol) == WaypointsService.ExtractSystemFromWaypoint(waypoint.Symbol))
    //             {
    //                 var tempFuelUsage = (int)Math.Ceiling(WaypointsService.CalculateDistance(lastWaypoint.X, lastWaypoint.Y, waypoint.X, waypoint.Y));
    //                 int cost = tempFuelUsage;
    //                 if (tempFuelUsage >= fuelMax)
    //                 {
    //                     cost = tempFuelUsage * tempFuelUsage;
    //                     tempFuelUsage = 1;
    //                 }
    //                 if (waypoint.Marketplace?.Exchange.ToList().Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
    //                 {
    //                     bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, fuelMax, waypointToSearch.Value.Item5 + cost);
    //                 }
    //                 else
    //                 {
    //                     bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3 + tempFuelUsage, currentFuel - tempFuelUsage, waypointToSearch.Value.Item5 + cost);
    //                 }
    //             }
    //             else
    //             {
    //                 bestPath[waypoint] = (tempPath, false, waypointToSearch.Value.Item3, fuelMax, waypointToSearch.Value.Item5 + JUMP_COST);
    //             }
    //         }
    //         bestPath[waypointToSearch.Key] = (waypointToSearch.Value.Item1, true, waypointToSearch.Value.Item3, waypointToSearch.Value.Item4, waypointToSearch.Value.Item5);
    //     }
    //     systemPath = bestPath.ToDictionary(p => p.Key.Symbol, p => (p.Value.Item1.Select(i => i.Symbol).ToList(), p.Value.Item5));

    //     await _pathsCacheService.SetSystemPathWithCost(currentWaypoint.Symbol, fuelMax, startingFuel, systemPath);
    //     return systemPath;
    // }

    // private readonly Dictionary<(string, int, int), Dictionary<string, ValueTuple<List<string>, int>>> SystemPathMemo = new ();
    // public async Task<Dictionary<string, ValueTuple<List<string>, int>>> BuildSystemPathWithCostWithMemo(
    //     string originWaypoint,
    //     int fuelMax,
    //     int startingFuel)
    // {
    //     if (SystemPathMemo.TryGetValue((originWaypoint, fuelMax, startingFuel), out var value))
    //     {
    //         return value;
    //     }
    //     var result = await BuildSystemPathWithCost(originWaypoint, fuelMax, startingFuel);
    //     SystemPathMemo.Add((originWaypoint, fuelMax, startingFuel), result);
    //     return result;
    // }

    public async Task<List<PathModel>> BuildSystemPathWithCost(
        string originWaypoint, 
        int maxFuel, 
        int startingFuel)
    {
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(originWaypoint));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        return BuildSystemPathWithCost(waypoints, originWaypoint, maxFuel, startingFuel);
    }

    public static List<PathModel> BuildSystemPathWithCost(
        List<Waypoint> waypoints,
        string originWaypoint, 
        int maxFuel, 
        int startingFuel)
    {
        const int COST_OF_JUMP = 1000;
        var waypointsReviewed = new List<string>();
        var waypointsCost = new List<PathModel>
        {
            new(originWaypoint, [originWaypoint], 0, startingFuel),
        };

        while (waypointsReviewed.Count < waypoints.Count)
        {
            var pathModelToReview = waypointsCost.Where(w => !waypointsReviewed.Contains(w.WaypointSymbol)).OrderBy(w => w.TimeCost).First();
            var currentFuel = pathModelToReview.ResultFuel;
            var waypointToReview = waypoints.Single(w => w.Symbol == pathModelToReview.WaypointSymbol);
            var waypointsWithinSystemNotReviewed = waypoints
                .Where(w => WaypointsService.ExtractSystemFromWaypoint(w.Symbol) == WaypointsService.ExtractSystemFromWaypoint(waypointToReview.Symbol)
                    && !waypointsReviewed.Contains(w.Symbol)
                    && w.Symbol != pathModelToReview.WaypointSymbol)
                .ToList();
            foreach (var waypointWithinSystem in waypointsWithinSystemNotReviewed)
            {
                var cost = CalculateCost(waypointToReview, waypointWithinSystem, currentFuel);
                if (HasRefuel(waypointWithinSystem)) 
                {
                    currentFuel = maxFuel;
                }
                else
                {
                    if (cost < currentFuel) currentFuel -= cost;
                    else currentFuel--;
                }
                ReplaceIfLowerCostOrAdd(waypointsCost, pathModelToReview, waypointWithinSystem, cost, currentFuel);
            }
            if (waypointToReview.JumpGate is not null && !waypointToReview.IsUnderConstruction)
            {
                var jumpGateConnectionsNotReviewed = waypointToReview.JumpGate.Connections.Where(c => !waypointsReviewed.Contains(c)).ToList();
                foreach (var jumpGateWaypoint in jumpGateConnectionsNotReviewed)
                {
                    var jumpGateConnectionWaypoint = waypoints.SingleOrDefault(w => w.Symbol == jumpGateWaypoint);
                    if (jumpGateConnectionWaypoint is null) continue;
                    if (HasRefuel(jumpGateConnectionWaypoint)) currentFuel = maxFuel;
                    ReplaceIfLowerCostOrAdd(waypointsCost, pathModelToReview, jumpGateConnectionWaypoint, COST_OF_JUMP, currentFuel);
                }
            }
            waypointsReviewed.Add(pathModelToReview.WaypointSymbol);
        }

        return waypointsCost;
    }

    private static void ReplaceIfLowerCostOrAdd(List<PathModel> waypointsCost, PathModel origin, Waypoint destination, int cost, int currentFuel)
    {
        var originPathModel = waypointsCost.Single(wc => wc.WaypointSymbol == origin.WaypointSymbol);
        var destinationPathModel = waypointsCost.SingleOrDefault(wc => wc.WaypointSymbol == destination.Symbol);
        if (destinationPathModel is null)
        {
            var clonedPath = new List<string>();
            clonedPath.AddRange(originPathModel.PathWaypointSymbols);
            clonedPath.AddRange(destination.Symbol);
            waypointsCost.Add(new PathModel(destination.Symbol, clonedPath, originPathModel.TimeCost + cost, currentFuel));
            return;
        }
        
        var newCost = originPathModel.TimeCost + cost;
        if (newCost < destinationPathModel.TimeCost)
        {
            var clonedPath = new List<string>();
            clonedPath.AddRange(originPathModel.PathWaypointSymbols);
            clonedPath.AddRange(destination.Symbol);
            waypointsCost.Remove(destinationPathModel);
            waypointsCost.Add(new PathModel(destination.Symbol, clonedPath, newCost, currentFuel));
        }
    }

    public static int CalculateCost(Waypoint origin, Waypoint destination, int currentFuel)
    {
        var distance = WaypointsService.CalculateDistance(origin.X, origin.Y, destination.X, destination.Y);
        if (distance > currentFuel)
        {
            return (int)Math.Ceiling(distance * 10);
        }
        return (int)Math.Ceiling(distance);
    }

    public static bool HasRefuel(Waypoint waypoint)
    {
        return waypoint.Marketplace?.Exchange.Any(e => e.Symbol == TradeSymbolsEnum.FUEL.ToString()) == true;
    }

    public static List<PathModelWithBurn> BuildSystemPathWithCostWithBurn(
        List<Waypoint> waypoints,
        string originWaypoint, 
        int maxFuel, 
        int startingFuel,
        string waypointShortCircuit = null,
        int depth = 10)
    {
        var waypointsDictionary = waypoints.ToDictionary(w => w.Symbol, w => w);
        const int COST_OF_JUMP = 1000;
        var waypointsReviewed = new List<string>();
        var waypointsCost = new List<PathModelWithBurn>
        {
            new(originWaypoint, [new (originWaypoint, NavFlightModeEnum.CRUISE)], 0, startingFuel),
        };
        while (waypointsReviewed.Count < waypoints.Count)
        {
            var pathModelToReview = waypointsCost.Where(w => !waypointsReviewed.Contains(w.WaypointSymbol)).OrderBy(w => w.TimeCost).First();
            var pathModelToReviewWaypoint = waypointsDictionary[pathModelToReview.WaypointSymbol];
            if (HasRefuel(pathModelToReviewWaypoint))
            {
                CleanupWaypointsCostIfHasRefuel(waypointsCost, pathModelToReview.WaypointSymbol);
            }

            var currentFuel = pathModelToReview.ResultFuel;
            var waypointToReview = waypointsDictionary[pathModelToReview.WaypointSymbol];
            var waypointsWithinSystemNotReviewed = waypoints
                .Where(w => WaypointsService.ExtractSystemFromWaypoint(w.Symbol) == WaypointsService.ExtractSystemFromWaypoint(waypointToReview.Symbol)
                    && !waypointsReviewed.Contains(w.Symbol)
                    && w.Symbol != pathModelToReview.WaypointSymbol)
                .ToList();
            
            foreach (var waypointWithinSystemNotReviewed in waypointsWithinSystemNotReviewed)
            {
                var waypointsCostTemp = new List<PathModelWithBurn>();
                var waypointCostsForWaypointWithinSystemNotReviewed = waypointsCost.Where(w => w.WaypointSymbol == waypointToReview.Symbol).ToList();
                foreach (var waypointCost in waypointCostsForWaypointWithinSystemNotReviewed)
                {
                    int? burnCost = GetBurnCost(waypointWithinSystemNotReviewed, waypointsDictionary[waypointCost.WaypointSymbol], currentFuel);
                    int? burnFuel = GetBurnFuel(waypointWithinSystemNotReviewed, waypointsDictionary[waypointCost.WaypointSymbol], currentFuel);
                    if (burnCost is not null && burnFuel is not null)
                    {
                        AddToWaypointsCost(waypointsCostTemp, waypointWithinSystemNotReviewed, waypointCost, NavFlightModeEnum.BURN, currentFuel - burnFuel.Value, burnCost.Value, maxFuel, currentFuel);
                    }
                    int? cruiseCost = GetCruiseCost(waypointWithinSystemNotReviewed, waypointsDictionary[waypointCost.WaypointSymbol], currentFuel);
                    int? cruiseFuel = GetCruiseFuel(waypointWithinSystemNotReviewed, waypointsDictionary[waypointCost.WaypointSymbol], currentFuel);
                    if (cruiseCost is not null && cruiseFuel is not null)
                    {
                        AddToWaypointsCost(waypointsCostTemp, waypointWithinSystemNotReviewed, waypointCost, NavFlightModeEnum.CRUISE, currentFuel - cruiseFuel.Value, cruiseCost.Value, maxFuel, currentFuel);
                    }
                    int driftCost = GetDriftCost(waypointWithinSystemNotReviewed, waypointsDictionary[waypointCost.WaypointSymbol]);
                    int driftFuel = GetDriftFuel();
                    if (burnCost is null && burnFuel is null && cruiseCost is null && cruiseFuel is null)
                    {
                        AddToWaypointsCost(waypointsCostTemp, waypointWithinSystemNotReviewed, waypointCost, NavFlightModeEnum.DRIFT, currentFuel - driftFuel, driftCost, maxFuel, currentFuel);
                    }
                }

                waypointsCost.AddRange(waypointsCostTemp);
            }

            if (waypointToReview.JumpGate is not null && !waypointToReview.IsUnderConstruction)
            {
                var jumpGateConnectionsNotReviewed = waypointToReview.JumpGate.Connections.Where(c => !waypointsReviewed.Contains(c)).ToList();
                foreach (var jumpGateConnectionNotReviewed in jumpGateConnectionsNotReviewed)
                {
                    var jumpGateConnectionNotReviewedWaypoint = waypointsDictionary[jumpGateConnectionNotReviewed];
                    if (jumpGateConnectionNotReviewedWaypoint is null) continue;
                    var newFuel = currentFuel;
                    if (HasRefuel(jumpGateConnectionNotReviewedWaypoint))
                    {
                        newFuel = maxFuel;
                    }
                    var newPathWaypoints = pathModelToReview.PathWaypoints.ToList();
                    newPathWaypoints.Add(new (jumpGateConnectionNotReviewed, NavFlightModeEnum.CRUISE));
                    waypointsCost.Add(new PathModelWithBurn(jumpGateConnectionNotReviewed, newPathWaypoints, pathModelToReview.TimeCost + COST_OF_JUMP, newFuel));
                }
            }

            waypointsCost = waypointsCost.GroupBy(w => w.WaypointSymbol).SelectMany(wc => wc.OrderBy(w => w.TimeCost).ThenBy(w => w.ResultFuel).Take(depth)).ToList();

            waypointsReviewed.Add(waypointToReview.Symbol);
            if (waypointToReview.Symbol == waypointShortCircuit) break;
        }
        return waypointsCost
            .GroupBy(w => w.WaypointSymbol)
            .Select(wg => 
                wg.OrderBy(w => w.TimeCost)
                .ThenByDescending(w => w.ResultFuel)
                .First()).ToList();
    }

    private static void AddToWaypointsCost(List<PathModelWithBurn> waypointsCostTemp, Waypoint waypointWithinSystemNotReviewed, PathModelWithBurn waypointCost, NavFlightModeEnum flightMode, int fuelCost, int timeCost, int maxFuel, int currentFuel)
    {
        var newPathWaypoints = waypointCost.PathWaypoints.ToList();
        newPathWaypoints.Add(new (waypointWithinSystemNotReviewed.Symbol, flightMode));
        var newFuel = HasRefuel(waypointWithinSystemNotReviewed) ? maxFuel : fuelCost;
        waypointsCostTemp.Add(new (waypointWithinSystemNotReviewed.Symbol, newPathWaypoints, waypointCost.TimeCost + timeCost, newFuel));
    }

    private static void CleanupWaypointsCostIfHasRefuel(List<PathModelWithBurn> waypointsCost, string waypointSymbol)
    {
        var waypointsToRemove = waypointsCost
            .Where(w => w.WaypointSymbol == waypointSymbol)
            .OrderBy(w => w.TimeCost)
            .ThenByDescending(w => w.ResultFuel)
            .Skip(1)
            .ToList();
        foreach (var waypointToRemove in waypointsToRemove.ToList())
        {
            waypointsCost.Remove(waypointToRemove);
        }
    }

    private static int? GetCruiseFuel(Waypoint originWaypoint, Waypoint destinationWaypoint, int currentFuel)
    {
        var cost = WaypointsService.CalculateDistance(originWaypoint, destinationWaypoint);
        var costInt = (int)Math.Ceiling(cost);
        if (costInt <= currentFuel) return costInt;
        return null;
    }

    private static int? GetCruiseCost(Waypoint originWaypoint, Waypoint destinationWaypoint, int currentFuel)
    {
        var cost = WaypointsService.CalculateDistance(originWaypoint, destinationWaypoint);
        var costInt = (int)Math.Ceiling(cost);
        if (costInt <= currentFuel) return costInt;
        return null;
    }

    private static int? GetBurnFuel(Waypoint originWaypoint, Waypoint destinationWaypoint, int currentFuel)
    {
        var cost = WaypointsService.CalculateDistance(originWaypoint, destinationWaypoint);
        var costInt = ((int)Math.Ceiling(cost)) * 2;
        if (costInt <= currentFuel) return costInt;
        return null;
    }

    private static int? GetBurnCost(Waypoint originWaypoint, Waypoint destinationWaypoint, int currentFuel)
    {
        var cost = WaypointsService.CalculateDistance(originWaypoint, destinationWaypoint);
        var costInt = ((int)Math.Ceiling(cost)) * 2;
        if (costInt <= currentFuel) return (int)Math.Ceiling(cost / 2);
        return null;
    }

    private static int GetDriftFuel()
    {
        return 1;
    }

    private static int GetDriftCost(Waypoint originWaypoint, Waypoint destinationWaypoint)
    {
        var cost = WaypointsService.CalculateDistance(originWaypoint, destinationWaypoint);
        var costInt = (int)Math.Ceiling(cost) * 10;
        return costInt;
    }
}