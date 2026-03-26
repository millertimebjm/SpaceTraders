using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class ExplorationCommand(
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
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if (!WaypointsService.IsVisited(currentWaypoint))
        {
            if (currentWaypoint.Traits.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()))
            {
                try
                {
                    var chartWaypointResult = await _shipsService.ChartAsync(ship.Symbol);
                    currentWaypoint = chartWaypointResult.Waypoint;
                    await _waypointsCacheService.SetAsync(currentWaypoint);
                    await _agentsService.SetAsync(chartWaypointResult.Agent);
                }
                catch (Exception ex)
                {
                    
                }
            }
        }
        else
        {
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        }
        
        if (ShipsService.GetShipCooldown(ship) is not null) return shipStatus;
        currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
        if (ship.Goal == ship.Nav.WaypointSymbol)
        {
            ship = ship with { Goal = null };
        }

        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return shipStatus;

            Nav? nav;
            Fuel? fuel;
            Cooldown cooldown = ship.Cooldown;
            string? goal = ship.Goal;
            
            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _transactionsService.SetAsync(refuelResponse.Transaction);
                continue;
            }

            nav = await _shipCommandsHelperService.DockForFuel(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);

                continue;
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            List<string> explorerShipGoals = 
                shipsDictionary
                .Values
                .Where(s => 
                    s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.Exploration 
                    && s.Goal is not null)
                .Select(s => s.Goal ?? "")
                .ToList();
            (nav, fuel, cooldown, goal) = await NavigateToExplore(ship, currentWaypoint, explorerShipGoals);
            if (nav is not null || fuel is not null)
            {
                ship = ship with { Nav = nav ?? ship.Nav, Fuel = fuel ?? ship.Fuel, Cooldown = cooldown, Goal = goal };
                return new ShipStatus(ship, $"Navigate To Explore {ship.Nav.WaypointSymbol}", DateTime.UtcNow);
            }

            ship = ship with { ShipCommand = null };
            return new ShipStatus(ship, $"No instructions set.", DateTime.UtcNow);
        }
    }

    private const int minimumFuel = 5;

    public async Task<(Nav? nav, Fuel? fuel, Cooldown cooldown, string? goal)> NavigateToExplore(
        Ship ship, 
        Waypoint currentWaypoint,
        List<string> otherShipGoals)
    {
        var systems = await _systemsService.GetAsync();
        var reachableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol, int.MaxValue);
        var waypoints = reachableSystems.SelectMany(s => s.Waypoints).ToList();
        var paths = await _pathsService.BuildSystemPathWithCostWithBurn2(reachableSystems.Select(s => s.Symbol).ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

        Nav? nav = null;
        Fuel? fuel = null;
        Cooldown cooldown = ship.Cooldown;

        if (ship.Fuel.Current < minimumFuel)
        {
            var fuelPaths = waypoints
                .Where(p => ship.Nav.SystemSymbol == WaypointsService.ExtractSystemFromWaypoint(p.Symbol) && 
                    p.Marketplace?.TradeGoods?.Any(tg => tg.Symbol == InventoryEnum.FUEL.ToString()) == true)
                .Select(w => w.Symbol)
                .ToList();
            var shortestWaypointSymbol = paths.Where(p => fuelPaths.Contains(p.WaypointSymbol)).OrderBy(p => p.TimeCost).FirstOrDefault();
            
            (nav, fuel) = await _shipsService.NavigateAsync(shortestWaypointSymbol.WaypointSymbol, ship);
            return (nav, fuel, ship.Cooldown, ship.Goal);
        }

        if (ship.Nav.WaypointSymbol == ship.Goal)
        {
            ship = ship with { Goal = null };
        }

        if (ship.Goal is not null)
        {
            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, ship.Goal);
            return (nav, fuel, cooldown, ship.Goal);
        }

        var fullSystems = otherShipGoals.GroupBy(g => WaypointsService.ExtractSystemFromWaypoint(g)).Where(s => s.Count() >= 3).Select(s => s.Key).ToList();

        var unmappedWaypoints = waypoints
            .Where(w =>
                !WaypointsService.IsMarketplaceVisited(w)
                && !otherShipGoals.Contains(w.Symbol)
                && !fullSystems.Contains(WaypointsService.ExtractSystemFromWaypoint(w.Symbol)))
            .Select(w => w.Symbol)
            .ToList();

        if (!unmappedWaypoints.Any())
        {
            unmappedWaypoints = waypoints
            .Where(w =>
                !WaypointsService.IsVisited(w)
                && !otherShipGoals.Contains(w.Symbol))
            .Select(w => w.Symbol)
            .ToList();
        }

        var unmappedPaths = paths.Where(p => unmappedWaypoints.Contains(p.WaypointSymbol)).ToList();
        var closestUnmappedPath = unmappedPaths
            .OrderByDescending(p => WaypointsService.ExtractSystemFromWaypoint(p.WaypointSymbol) == ship.Nav.SystemSymbol)
            .ThenBy(p => p.TimeCost)
            .ThenBy(p => p.WaypointSymbol)
            .FirstOrDefault();        

        if (closestUnmappedPath is null) return (nav, fuel, cooldown, null);
        var goal = closestUnmappedPath.WaypointSymbol;

        (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, closestUnmappedPath.WaypointSymbol);
        return (nav, fuel, cooldown, goal);
    }
}