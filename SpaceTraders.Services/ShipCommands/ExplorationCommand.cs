using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Paths;
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
    ISystemsService _systemsService) : IShipCommandsService
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
                }
                catch
                {

                }
            }
        }
        else
        {
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        }
        
        if (ShipsService.GetShipCooldown(ship) is not null) return shipStatus;
        //await Task.Delay(500);
        currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
        // Exploration goal can be for marketplace or uncharted
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
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown, Goal = goal };
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
        var reachableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = reachableSystems.SelectMany(s => s.Waypoints).ToList();
        var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints, currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

        Nav? nav;
        Fuel? fuel;
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
            var path = paths.SingleOrDefault(p => p.WaypointSymbol == ship.Goal);
            if (WaypointsService.ExtractSystemFromWaypoint(path.PathWaypoints[1].WaypointSymbol) != ship.Nav.SystemSymbol)
            {
                (nav, cooldown) = await _shipsService.JumpAsync(ship.Symbol, path.PathWaypoints[1].WaypointSymbol);
                return (nav, ship.Fuel, cooldown, ship.Goal);
            }
            (nav, fuel, cooldown) = await NavigateHelper(ship, path.PathWaypoints[1].WaypointSymbol);
            return (nav, fuel, cooldown, ship.Goal);
        }

        var unmappedWaypoints = waypoints
            .Where(w =>
                !WaypointsService.IsMarketplaceVisited(w)
                && !otherShipGoals.Contains(w.Symbol))
            .Select(w => w.Symbol)
            .ToList();

        
        var unmappedPaths = paths.Where(p => unmappedWaypoints.Contains(p.WaypointSymbol)).ToList();

        var closestUnmappedPath = unmappedPaths
            .OrderByDescending(p => WaypointsService.ExtractSystemFromWaypoint(p.WaypointSymbol) == ship.Nav.SystemSymbol)
            .ThenBy(p => p.TimeCost)
            .ThenBy(p => p.WaypointSymbol)
            .FirstOrDefault();

        var goal = closestUnmappedPath.WaypointSymbol;

        if (WaypointsService.ExtractSystemFromWaypoint(closestUnmappedPath.PathWaypoints[1].WaypointSymbol) != ship.Nav.SystemSymbol)
        {
            (nav, cooldown) = await _shipsService.JumpAsync(closestUnmappedPath.PathWaypoints[1].WaypointSymbol, ship.Symbol);
            return (nav, ship.Fuel, cooldown, goal);
        }
        (nav, fuel) = await _shipsService.NavigateAsync(closestUnmappedPath.PathWaypoints[1].WaypointSymbol, ship);
        return (nav, fuel, ship.Cooldown, goal);
    }

    private async Task<(Nav?, Fuel?, Cooldown)> NavigateHelper(Ship ship, string waypointSymbol)
    {
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints, ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current, waypointSymbol);
        var path = paths.Single(p => p.WaypointSymbol == waypointSymbol);
        var nextHop = path.PathWaypoints[1];

        Nav? nav = null;
        Fuel? fuel = null;
        Cooldown cooldown = ship.Cooldown;

        if (WaypointsService.ExtractSystemFromWaypoint(nextHop.WaypointSymbol) != WaypointsService.ExtractSystemFromWaypoint(waypointSymbol))
        {
            (nav, cooldown) = await _shipsService.JumpAsync(nextHop.WaypointSymbol, ship.Symbol);
            return (nav, fuel, cooldown);
        }
        nav = await _shipsService.NavToggleAsync(ship, nextHop.FlightModeEnum);
        (nav, fuel) = await _shipsService.NavigateAsync(nextHop.WaypointSymbol, ship);
        return (nav, fuel, cooldown);
    }
}