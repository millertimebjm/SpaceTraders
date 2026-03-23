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
    private const int COST_OF_JUMP = 1000;

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
                    if (!waypointsDictionary.TryGetValue(jumpGateConnectionNotReviewed, out var jumpGateConnectionNotReviewedWaypoint)) continue;
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

    public async Task<List<PathModelWithBurn>> BuildSystemPathWithCostWithBurn2(List<string> systemSymbols, string originWaypoint, int maxFuel, int currentFuel)
    {
        var systems = await _systemsService.GetAsync(systemSymbols);
        int maxRetries = 5;
        for (int i = 0; i < maxRetries; i++)
        {
            systems = await _systemsService.GetAsync(systemSymbols);
            if (systems.Any()) break;
            
            await Task.Delay(200);
        }
        var currentSystem = systems.Single(s => s.Symbol == WaypointsService.ExtractSystemFromWaypoint(originWaypoint)); 
        var pathModels = BuildSystemPathWithCostWithBurn(currentSystem.Waypoints.ToList(), originWaypoint, maxFuel, currentFuel);

        var workingCopySystemSymbols = systemSymbols.ToList();
        workingCopySystemSymbols.Remove(currentSystem.Symbol);
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();

        while (workingCopySystemSymbols.Any())
        {
            var currentSystemJumpGateWaypoint = currentSystem.Waypoints.SingleOrDefault(w => w.JumpGate is not null);
            if (currentSystemJumpGateWaypoint is null)
            {
                
            }
            var jumpGateConnectionSystemSymbols = currentSystemJumpGateWaypoint.JumpGate.Connections.Select(c => WaypointsService.ExtractSystemFromWaypoint(c)).ToList();
            var connectedSystemSymbols = systemSymbols.Where(ss => jumpGateConnectionSystemSymbols.Contains(ss)).ToList();
            var connectedSystems = systems.Where(s => connectedSystemSymbols.Contains(s.Symbol) && workingCopySystemSymbols.Contains(s.Symbol)).ToList();
            
            foreach (var connectedSystem in connectedSystems)
            {
                var connectedSystemJumpGate = connectedSystem.Waypoints.SingleOrDefault(w => w.JumpGate is not null)?.Symbol;
                if (connectedSystemJumpGate is null) 
                {
                    workingCopySystemSymbols.Remove(connectedSystem.Symbol);
                    continue;
                }

                var inSystemPathModels = await MemoizeSystemTravel(connectedSystemJumpGate, maxFuel, currentFuel);
                var expandSystemPathModels = AddNewSystemTravel(pathModels.Single(p => p.WaypointSymbol == currentSystemJumpGateWaypoint.Symbol), inSystemPathModels);
                pathModels.AddRange(expandSystemPathModels);
                workingCopySystemSymbols.Remove(connectedSystem.Symbol);
            }

            var pathModelsThatAreJumpGates = pathModels.Where(p => waypoints.Any(w => w.JumpGate is not null && w.Symbol == p.WaypointSymbol && w.JumpGate.Connections.Any(c => workingCopySystemSymbols.Contains(WaypointsService.ExtractSystemFromWaypoint(c))))).ToList();
            var minimumPathModelJumpGate = pathModelsThatAreJumpGates.FirstOrDefault(p => p.TimeCost == pathModelsThatAreJumpGates.Min(p => p.TimeCost));
            if (minimumPathModelJumpGate is not null) // If null, no more workingCopySystemSymbols
            {
                currentSystem = systems.Single(s => s.Symbol == WaypointsService.ExtractSystemFromWaypoint(minimumPathModelJumpGate.WaypointSymbol));
            }
        }
        return pathModels;
    }

    private static List<PathModelWithBurn> AddNewSystemTravel(PathModelWithBurn oldPathModel, List<PathModelWithBurn> newSystemPathModels)
    {
        
        // var connectionPathModels = oldPathModels.Where(p => connectionWaypointSymbols.Any(w => w == p.WaypointSymbol));
        // var shortestConnectionPathModel = connectionPathModels.OrderBy(p => p.TimeCost).First();
        List<PathModelWithBurn> newSystemUpdatedPathModels = [];
        foreach (var newPathModel in newSystemPathModels)
        {
            var templatePathWaypoints = oldPathModel.PathWaypoints.ToList();
            templatePathWaypoints.AddRange(newPathModel.PathWaypoints);
            newSystemUpdatedPathModels.Add(new PathModelWithBurn(newPathModel.WaypointSymbol, templatePathWaypoints, oldPathModel.TimeCost + newPathModel.TimeCost + COST_OF_JUMP, newPathModel.ResultFuel));
        }
        return newSystemUpdatedPathModels;
    }

    private async Task<List<PathModelWithBurn>> MemoizeSystemTravel(string startingJumpGateWaypointSymbol, int maxFuel, int currentFuel)
    {
        var key = $"{startingJumpGateWaypointSymbol}-{maxFuel}-{currentFuel}";
        var memoizeSystemTravel = await _pathsCacheService.GetMemoizeSystemTravel(key);
        if (memoizeSystemTravel is not null) return memoizeSystemTravel;

        var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(startingJumpGateWaypointSymbol));
        var waypoints = system.Waypoints.ToList();
        var systemPath = PathsService.BuildSystemPathWithCostWithBurn(waypoints, startingJumpGateWaypointSymbol, maxFuel, currentFuel);
        await _pathsCacheService.SetMemoizeSystemTravel(key, systemPath);
        return systemPath;
    }
}
