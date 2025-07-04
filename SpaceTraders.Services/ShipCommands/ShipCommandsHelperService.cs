using System.Linq;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

// MiningToSell,
// MiningToConstruction,
// MiningToStorage,
// CarryToConstruction,
// BuyToSell,
// BuyToConstruction,
// BuyToStorage,

public class ShipCommandsHelperService : IShipCommandsHelperService
{
    private readonly IShipsService _shipsService;
    private readonly IMarketplacesService _marketplacesService;
    private readonly ISystemsService _systemsService;
    private readonly IWaypointsService _waypointsService;
    private readonly IAgentsService _agentsService;
    public ShipCommandsHelperService(
        IShipsService shipsService,
        IMarketplacesService marketplacesService,
        ISystemsService systemsService,
        IWaypointsService waypointsService,
        IAgentsService agentsService)
    {
        _shipsService = shipsService;
        _marketplacesService = marketplacesService;
        _systemsService = systemsService;
        _waypointsService = waypointsService;
        _agentsService = agentsService;
    }

    public async Task<bool> Buy(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count > 0
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Exports.Any()))
        {
            return false;
        }

        var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var exports = currentWaypoint.Marketplace.Exports.Select(e => e.Symbol).ToList();

        // Only get those Export Trade Goods that are imports in a reachable waypoint in the system
        var inventoryToBuy = currentWaypoint
            .Marketplace
            .TradeGoods
            .Where(tg => exports.Contains(tg.Symbol) && (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.MODERATE
                && system.Waypoints.Where(w => paths.Select(p => p.Key.Symbol).Contains(w.Symbol)).Any(w => w.Marketplace is not null && w.Marketplace.Imports.Select(i => i.Symbol).Contains(tg.Symbol) ))
            .OrderByDescending(tg => (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply))
            .FirstOrDefault();

        if (inventoryToBuy is null) return false;
        // var agent = await _agentsService.GetAsync();
        // var creditsAvailable = agent.Credits;
        // var maxAmountToBuy = ((int)creditsAvailable - 10000) / inventoryToBuy.PurchasePrice;
        //await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, Math.Min(maxAmountToBuy, Math.Min(inventoryToBuy.TradeVolume, ship.Cargo.Capacity)));
        await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, Math.Min(inventoryToBuy.TradeVolume, ship.Cargo.Capacity));

        return true;
    }

    public async Task<bool> Dock(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return false;
        }

        var shouldDock = false;

        // Need fuel and fuel is available at this waypoint
        if (ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.Exchange.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
        {
            shouldDock = true;
        }
        // Have cargo and the market wants any of it
        else if (ship.Cargo.Units > 0 && currentWaypoint.Marketplace is not null)
        {
            var market = currentWaypoint.Marketplace;

            shouldDock = ship.Cargo.Inventory.Any(cargo =>
                market.Imports.Any(i => i.Symbol == cargo.Symbol) ||
                market.Exchange.Any(e => e.Symbol == cargo.Symbol));
        }
        else if (ship.Cargo.Units == 0 && currentWaypoint.Marketplace is not null)
        {
            var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
            var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
            var exports = currentWaypoint.Marketplace.Exports.Select(e => e.Symbol).ToList();

            var inventoryToBuy = currentWaypoint
            .Marketplace
            .TradeGoods
            .Where(tg => exports.Contains(tg.Symbol) && (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.MODERATE
                && system.Waypoints.Where(w => paths.Select(p => p.Key.Symbol).Contains(w.Symbol)).Any(w => w.Marketplace is not null && w.Marketplace.Imports.Select(i => i.Symbol).Contains(tg.Symbol) ))
            .OrderByDescending(tg => (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply))
            .FirstOrDefault();
        }

        if (!shouldDock)
            {
                return false;
            }

        await _shipsService.DockAsync(ship.Symbol);
        return true;
    }

    public async Task<DateTime?> Extract(Ship ship, Waypoint currentWaypoint, Waypoint miningWaypoint)
    {
        if (ship.Cargo.Units == ship.Cargo.Capacity
            || currentWaypoint.Symbol != miningWaypoint.Symbol)
        {
            return null;
        }

        var extractionResult = await _shipsService.ExtractAsync(ship.Symbol);
        return extractionResult.Cooldown.Expiration;
    }

    public async Task<DateTime?> NavigateToEndWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint)
    {
        if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString()
            || ship.Cargo.Units == 0
            || currentWaypoint.Symbol == endWaypoint.Symbol)
        {
            return null;
        }

        var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.Key.Symbol == endWaypoint.Symbol);

        var nav = await _shipsService.NavigateAsync(pathItem.Value.Item1[1].Symbol, ship.Symbol);
        return nav.Route.Arrival;
    }

    public async Task<DateTime?> NavigateToStartWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint startWaypoint)
    {
        if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString()
            || ship.Cargo.Units != 0
            || currentWaypoint.Symbol == startWaypoint.Symbol)
        {
            return null;
        }

        var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.Key.Symbol == startWaypoint.Symbol);

        var nav = await _shipsService.NavigateAsync(pathItem.Value.Item1[1].Symbol, ship.Symbol);

        return nav.Route.Arrival;
    }

    public async Task<bool> Orbit(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || currentWaypoint.Marketplace?.Imports.Any(i => ship.Cargo.Inventory.Select(inv => inv.Symbol).Contains(i.Symbol)) == true
            || ship.Fuel.Current != ship.Fuel.Capacity)
        {
            return false;
        }

        await _shipsService.OrbitAsync(ship.Symbol);
        return true;
    }

    public async Task<bool> Refuel(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Fuel.Current == ship.Fuel.Capacity
            || ship.Nav.Status != NavStatusEnum.DOCKED.ToString()
            || currentWaypoint.Marketplace is null
            || !currentWaypoint.Marketplace.Exchange.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()))
        {
            return false;
        }

        await _marketplacesService.RefuelAsync(ship.Symbol);
        return true;
    }

    public async Task<bool> Sell(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Imports.Any(i => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(i.Symbol)))
            && !currentWaypoint.Marketplace.Exchange.Any(e => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(e.Symbol)))
        {
            return false;
        }

        var originalCargoCount = ship.Cargo.Inventory.Count;
        foreach (var inventory in ship.Cargo.Inventory)
        {
            if (currentWaypoint.Marketplace.Imports.Any(i => i.Symbol == inventory.Symbol))
            {
                var tradeGood = currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == inventory.Symbol);
                var units = Math.Min(tradeGood.TradeVolume, inventory.Units);
                await _marketplacesService.SellAsync(ship.Symbol, inventory.Symbol, units);
                await Task.Delay(1000);
            }
            if (currentWaypoint.Marketplace.Exchange.Any(e => e.Symbol == inventory.Symbol))
            {
                await _marketplacesService.SellAsync(ship.Symbol, inventory.Symbol, inventory.Units);
                await Task.Delay(1000);
            }
        }
        return true;
    }

    public Task<bool> SupplyConstructionSite(Ship ship, Waypoint currentWaypoint)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Jettison(Ship ship)
    {
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);

        if (ship.Cargo.Inventory.Count == 0)
        {
            return false;
        }

        var allMarketplaces = system.Waypoints
            .Where(w => w.Marketplace is not null)
            .Select(w => w.Marketplace!)
            .ToList();

        var jettisonedAnything = false;

        foreach (var inventory in ship.Cargo.Inventory)
        {
            // Skip fuel — we don't want to jettison that
            if (inventory.Symbol == InventoryEnum.FUEL.ToString())
                continue;

            var isWantedInSystem = allMarketplaces.Any(m =>
                m.Imports.Any(i => i.Symbol == inventory.Symbol) ||
                m.Exchange.Any(e => e.Symbol == inventory.Symbol));

            if (!isWantedInSystem)
            {
                await _shipsService.JettisonAsync(ship.Symbol, inventory.Symbol, inventory.Units);
                jettisonedAnything = true;
            }
        }

        return jettisonedAnything;
    }

    public async Task<DateTime?> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0)
        {
            return null;
        }

        var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.Key.Symbol == endWaypoint.Symbol);

        var nav = await _shipsService.NavigateAsync(pathItem.Value.Item1[1].Symbol, ship.Symbol);

        return nav.Route.Arrival;
    }

    public async Task<Waypoint?> GetClosestSellingWaypoint(Ship ship, Waypoint currentWaypoint)
    {
        var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
        var inventorySymbols = ship.Cargo.Inventory.Select(i => i.Symbol).ToHashSet();
        var sellingWaypoints = system.Waypoints
            .Where(w => w.Marketplace is not null
                && w.Marketplace.Imports.Count(i => inventorySymbols.Contains(i.Symbol)) > 0)
            .OrderByDescending(w =>
                w.Marketplace.Imports.Count(i => inventorySymbols.Contains(i.Symbol)))
            .ToList();

        if (!sellingWaypoints.Any())
        {
            sellingWaypoints = system.Waypoints
                .Where(w => w.Marketplace is not null
                    && w.Marketplace.Exchange.Count(e => inventorySymbols.Contains(e.Symbol)) > 0)
                .OrderByDescending(w =>
                    w.Marketplace.Exchange.Count(e => inventorySymbols.Contains(e.Symbol)))
                .ToList();
        }

        if (!sellingWaypoints.Any()) return null;
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var shortestPath = paths.Where(p => sellingWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol)).ToList();
        return shortestPath.OrderBy(p => p.Value.Item1.Count).FirstOrDefault().Key;
    }
}