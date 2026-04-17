using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class CompleteOtherConstruction(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    ITransactionsCacheService _transactionsService,
    IShipsService _shipsService,
    IWaypointsCacheService _waypointsCacheService,
    ISystemsService _systemsService,
    IAgentsService _agentsService, 
    IPathsService _pathsService) : IShipCommandsService
{
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        if (ShipsService.GetShipCooldown(ship) is not null) return shipStatus;

        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if (currentWaypoint.Marketplace is not null
            || currentWaypoint.Shipyard is not null
            || (currentWaypoint.Construction is not null && currentWaypoint.IsUnderConstruction))
        {
            currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
        }

        if (ship.GoalModel is null)
        {
            var goalModel = await _shipCommandsHelperService.CompleteOtherConstructionGoalModel(ship, shipsDictionary);
            ship = ship with { GoalModel = goalModel };
        }
        if (ship.GoalModel is null)
        {
            ship = ship with { Cooldown = new Cooldown(ship.Symbol, 60 * 10, 60 * 10, DateTime.UtcNow.AddMinutes(10)) };
            return new ShipStatus(ship, "GoalModel stayed null.", DateTime.UtcNow);
        }

        Nav nav = ship.Nav;
        Fuel fuel = ship.Fuel;
        Cooldown cooldown = ship.Cooldown;

        if (ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.TradeGoods?.Any(tg => tg.Symbol == TradeSymbolsEnum.FUEL.ToString()) == true)
        {

            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is null) throw new SpaceTraderResultException("Refuel failed");
            ship = ship with { Fuel = refuelResponse.Fuel };
            await _agentsService.SetAsync(refuelResponse.Agent);
            await _transactionsService.SetAsync(refuelResponse.Transaction);
        }

        if (ship.Nav.SystemSymbol != ship.GoalModel.BuyWaypointSymbol
            && ship.Nav.WaypointSymbol != ship.GoalModel.SellWaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, ship.GoalModel.SellWaypointSymbol!);
            ship = ship with { Nav = nav ?? ship.Nav, Fuel = fuel ?? ship.Fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To closest jump gate {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }

        if (ship.Nav.SystemSymbol != ship.GoalModel.BuyWaypointSymbol
            && ship.Nav.WaypointSymbol == ship.GoalModel.SellWaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            if (ship.Cargo.Units == 0)
            {
                var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(ship, currentWaypoint, TradeSymbolsEnum.FUEL.ToString(), ship.Cargo.Capacity);
                ship = ship with { Cargo = purchaseCargoResult.Cargo};
                await _agentsService.SetAsync(purchaseCargoResult.Agent);
                await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
            }

            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            
            nav = await _shipsService.NavToggleAsync(ship, NavFlightModeEnum.DRIFT);
            ship = ship with { Nav = nav };
            
            var systems = await _systemsService.GetAsync();
            var goalSystem = systems.Single(s => s.Symbol == ship.GoalModel.BuyWaypointSymbol);
            var goalJumpGate = goalSystem.Waypoints.Single(s => s.JumpGate is not null);
            (nav, fuel) = await _shipCommandsHelperService.WarpAsync(ship, goalJumpGate);
            ship = ship with { Nav = nav ?? ship.Nav, Fuel = fuel ?? ship.Fuel};
            return new ShipStatus(ship, $"Warping to unfinished jump gate {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }

        if (ship.Nav.SystemSymbol == ship.GoalModel.BuyWaypointSymbol)
        {
            ship = ship with { GoalModel = null, ShipCommand = new ShipCommand(ship.Symbol, ShipCommandEnum.SupplyConstruction) };
            return new ShipStatus(ship, "Ready to Supply Construction.", DateTime.UtcNow);
        }

        ship = ship with { Cooldown = new Cooldown(ship.Symbol, 60 * 10, 60 * 10, DateTime.UtcNow.AddMinutes(10)) };
        return new ShipStatus(ship, "CompleteOtherConstruction not ready yet.", DateTime.UtcNow);

        // if (!WaypointsService.IsVisited(currentWaypoint))
        // {
        //     if (currentWaypoint.Traits.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()))
        //     {
        //         try
        //         {
        //             var chartWaypointResult = await _shipsService.ChartAsync(ship.Symbol);
        //             currentWaypoint = chartWaypointResult.Waypoint;
        //             await _waypointsCacheService.SetAsync(currentWaypoint);
        //             await _agentsService.SetAsync(chartWaypointResult.Agent);
        //         }
        //         catch (Exception ex)
        //         {
        //             throw new SpaceTraderResultException(ex.Message);
        //         }
        //     }
        // }
        

    }

        // if (ship.Goal == ship.Nav.WaypointSymbol)
        // {
        //     ship = ship with { Goal = null };
        // }

    //     while (true)
    //     {
    //         if (ShipsService.GetShipCooldown(ship) is not null) return shipStatus;

    //         Nav? nav;
    //         Fuel? fuel;
    //         Cooldown cooldown = ship.Cooldown;
    //         string? goal = ship.Goal;
            
    //         var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
    //         if (refuelResponse is not null)
    //         {
    //             ship = ship with { Fuel = refuelResponse.Fuel };
    //             await _transactionsService.SetAsync(refuelResponse.Transaction);
    //             continue;
    //         }

    //         nav = await _shipCommandsHelperService.DockForFuel(ship, currentWaypoint);
    //         if (nav is not null)
    //         {
    //             ship = ship with { Nav = nav };
    //             currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);

    //             continue;
    //         }

    //         nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
    //         if (nav is not null)
    //         {
    //             ship = ship with { Nav = nav };
    //             continue;
    //         }

    //         List<string> explorerShipGoals = 
    //             shipsDictionary
    //             .Values
    //             .Where(s => 
    //                 s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.Exploration 
    //                 && s.Goal is not null)
    //             .Select(s => s.Goal ?? "")
    //             .ToList();
    //         (nav, fuel, cooldown, goal) = await NavigateToExplore(ship, currentWaypoint, explorerShipGoals);
    //         if (nav is not null || fuel is not null)
    //         {
    //             ship = ship with { Nav = nav ?? ship.Nav, Fuel = fuel ?? ship.Fuel, Cooldown = cooldown, Goal = goal };
    //             return new ShipStatus(ship, $"Navigate To Explore {ship.Nav.WaypointSymbol}", DateTime.UtcNow);
    //         }

    //         ship = ship with { ShipCommand = null };
    //         return new ShipStatus(ship, $"No instructions set.", DateTime.UtcNow);
    //     }
    // }

    // private const int minimumFuel = 5;

    // public async Task<(Nav? nav, Fuel? fuel, Cooldown cooldown, string? goal)> NavigateToExplore(
    //     Ship ship, 
    //     Waypoint currentWaypoint,
    //     List<string> otherShipGoals)
    // {
    //     var systems = await _systemsService.GetAsync();
    //     var reachableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol, int.MaxValue);
    //     var waypoints = reachableSystems.SelectMany(s => s.Waypoints).ToList();
    //     var paths = await _pathsService.BuildSystemPathWithCostWithBurn2(reachableSystems.Select(s => s.Symbol).ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

    //     Nav? nav = null;
    //     Fuel? fuel = null;
    //     Cooldown cooldown = ship.Cooldown;

    //     if (ship.Fuel.Current < minimumFuel)
    //     {
    //         var fuelPaths = waypoints
    //             .Where(p => ship.Nav.SystemSymbol == WaypointsService.ExtractSystemFromWaypoint(p.Symbol) && 
    //                 p.Marketplace?.TradeGoods?.Any(tg => tg.Symbol == InventoryEnum.FUEL.ToString()) == true)
    //             .Select(w => w.Symbol)
    //             .ToList();
    //         var shortestWaypointSymbol = paths
    //             .Where(p => fuelPaths.Contains(p.WaypointSymbol))
    //             .OrderBy(p => p.TimeCost)
    //             .First();
            
    //         (nav, fuel) = await _shipsService.NavigateAsync(shortestWaypointSymbol.WaypointSymbol, ship);
    //         return (nav, fuel, ship.Cooldown, ship.Goal);
    //     }

    //     if (ship.Nav.WaypointSymbol == ship.Goal)
    //     {
    //         ship = ship with { Goal = null };
    //     }

    //     if (ship.Goal is not null)
    //     {
    //         (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, ship.Goal);
    //         return (nav, fuel, cooldown, ship.Goal);
    //     }

    //     var fullSystems = otherShipGoals.GroupBy(g => WaypointsService.ExtractSystemFromWaypoint(g)).Where(s => s.Count() >= 3).Select(s => s.Key).ToList();

    //     var unmappedWaypoints = waypoints
    //         .Where(w =>
    //             !WaypointsService.IsMarketplaceVisited(w)
    //             && !otherShipGoals.Contains(w.Symbol)
    //             && !fullSystems.Contains(WaypointsService.ExtractSystemFromWaypoint(w.Symbol)))
    //         .Select(w => w.Symbol)
    //         .ToList();

    //     if (!unmappedWaypoints.Any())
    //     {
    //         unmappedWaypoints = waypoints
    //         .Where(w =>
    //             !WaypointsService.IsVisited(w)
    //             && !otherShipGoals.Contains(w.Symbol))
    //         .Select(w => w.Symbol)
    //         .ToList();
    //     }

    //     var unmappedPaths = paths.Where(p => unmappedWaypoints.Contains(p.WaypointSymbol)).ToList();
    //     var closestUnmappedPath = unmappedPaths
    //         .OrderByDescending(p => WaypointsService.ExtractSystemFromWaypoint(p.WaypointSymbol) == ship.Nav.SystemSymbol)
    //         .ThenBy(p => p.TimeCost)
    //         .ThenBy(p => p.WaypointSymbol)
    //         .FirstOrDefault();        

    //     if (closestUnmappedPath is null) return (nav, fuel, cooldown, null);
    //     var goal = closestUnmappedPath.WaypointSymbol;

    //     (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, closestUnmappedPath.WaypointSymbol);
    //     return (nav, fuel, cooldown, goal);
    // }
}