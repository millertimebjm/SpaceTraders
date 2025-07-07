using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Surveys.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
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
    private readonly IConstructionsService _constructionService;
    private readonly ISurveysCacheService _surveysCacheService;
    private readonly IShipyardsService _shipyardsService;
    public ShipCommandsHelperService(
        IShipsService shipsService,
        IMarketplacesService marketplacesService,
        ISystemsService systemsService,
        IWaypointsService waypointsService,
        IAgentsService agentsService,
        IConstructionsService constructionsService,
        ISurveysCacheService surveysCacheService,
        IShipyardsService shipyardsService)
    {
        _shipsService = shipsService;
        _marketplacesService = marketplacesService;
        _systemsService = systemsService;
        _waypointsService = waypointsService;
        _agentsService = agentsService;
        _constructionService = constructionsService;
        _surveysCacheService = surveysCacheService;
        _shipyardsService = shipyardsService;
    }

    public async Task<Cargo?> Buy(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count > 0
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Exports.Any()))
        {
            return null;
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var exports = currentWaypoint.Marketplace.Exports.Select(e => e.Symbol).ToList();

        // Only get those Export Trade Goods that are imports in a reachable waypoint in the system
        // var inventoryToBuy = currentWaypoint
        //     .Marketplace
        //     .TradeGoods
        //     .Where(tg => exports.Contains(tg.Symbol) && (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.MODERATE
        //         && system.Waypoints.Where(w => paths.Select(p => p.Key.Symbol).Contains(w.Symbol)).Any(w => w.Marketplace is not null && w.Marketplace.Imports.Select(i => i.Symbol).Contains(tg.Symbol)))
        //     .OrderByDescending(tg => (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply))
        //     .FirstOrDefault();

        // if (inventoryToBuy is null) return null;


        // var agent = await _agentsService.GetAsync();
        // var creditsAvailable = agent.Credits;
        // var maxAmountToBuy = ((int)creditsAvailable - 10000) / inventoryToBuy.PurchasePrice;
        //await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, Math.Min(maxAmountToBuy, Math.Min(inventoryToBuy.TradeVolume, ship.Cargo.Capacity)));

        Cargo? cargo = null;
        while (ship.Cargo.Units < ship.Cargo.Capacity)
        {
            var inventoryToBuy = currentWaypoint
            .Marketplace
            .TradeGoods
            .Where(tg => exports.Contains(tg.Symbol) && (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.MODERATE
                && system.Waypoints.Where(w => paths.Select(p => p.Key.Symbol).Contains(w.Symbol)).Any(w => w.Marketplace is not null && w.Marketplace.Imports.Select(i => i.Symbol).Contains(tg.Symbol)))
            .OrderByDescending(tg => (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply))
            .FirstOrDefault();

            if (inventoryToBuy is null) break;

            cargo = await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, Math.Min(inventoryToBuy.TradeVolume, ship.Cargo.Capacity - ship.Cargo.Units));
            ship = ship with { Cargo = cargo };
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        }

        return cargo;
    }

    public async Task<Cargo?> BuyForConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint)
    {
        var constructionInventoryNeeded = constructionWaypoint.Construction
            .Materials
            .Where(m => m.Required - m.Fulfilled > 0).ToList();
        var constructionInventoryNeededSymbols = constructionInventoryNeeded.Select(m => m.TradeSymbol);

        if (ship.Cargo.Inventory.Count > 0
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Exports.Any(e => constructionInventoryNeededSymbols.Contains(e.Symbol))))
        {
            return null;
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var exports = currentWaypoint.Marketplace.Exports.Select(e => e.Symbol).ToList();

        // Only get those Export Trade Goods that are imports in a reachable waypoint in the system
        var inventoryToBuy = currentWaypoint
            .Marketplace
            .TradeGoods
            .Where(tg => exports.Contains(tg.Symbol)
                && constructionInventoryNeededSymbols.Contains(tg.Symbol))
            .OrderByDescending(tg => (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply))
            .FirstOrDefault();

        if (inventoryToBuy is null) return null;
        var constructionInventory = constructionInventoryNeeded.Single(cin => cin.TradeSymbol == inventoryToBuy.Symbol);
        var cargo = await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, Math.Min(constructionInventory.Required - constructionInventory.Fulfilled, Math.Min(inventoryToBuy.TradeVolume, ship.Cargo.Capacity)));

        await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        return cargo;
    }

    public async Task<Nav?> DockForFuel(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return null;
        }

        var shouldDock = false;

        // Need fuel and fuel is available at this waypoint
        if (ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.Exchange.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
        {
            shouldDock = true;
        }

        if (!shouldDock)
        {
            return null;
        }

        return await _shipsService.DockAsync(ship.Symbol);
    }

    public async Task<Nav?> DockForShipyard(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return null;
        }

        var shouldDock = false;
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var ships = await _shipsService.GetAsync();
        var shipToBuy = await ShipToBuy(ships, system);
        if (shipToBuy is null) return null;

        // Need fuel and fuel is available at this waypoint
        if ((ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.Exchange.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
            || currentWaypoint.Shipyard?.ShipTypes.Any(st => st.Type == shipToBuy.ToString()) == true)
        {
            shouldDock = true;
        }

        if (!shouldDock)
        {
            return null;
        }

        return await _shipsService.DockAsync(ship.Symbol);
    }

    public async Task<Nav?> DockForMiningToSellAnywhere(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return null;
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

        if (!shouldDock)
        {
            return null;
        }

        return await _shipsService.DockAsync(ship.Symbol);
    }

    public async Task<Nav?> DockForBuyAndSell(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return null;
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
            var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
            var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
            var exports = currentWaypoint.Marketplace.Exports.Select(e => e.Symbol).ToList();

            var inventoryToBuy = currentWaypoint
            .Marketplace
            .TradeGoods
            .Where(tg => exports.Contains(tg.Symbol) && (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.MODERATE
                && system.Waypoints.Where(w => paths.Select(p => p.Key.Symbol).Contains(w.Symbol)).Any(w => w.Marketplace is not null && w.Marketplace.Imports.Select(i => i.Symbol).Contains(tg.Symbol)))
            .OrderByDescending(tg => (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply))
            .FirstOrDefault();
            if (inventoryToBuy is not null)
            {
                shouldDock = true;
            }
        }

        if (!shouldDock)
        {
            return null;
        }

        return await _shipsService.DockAsync(ship.Symbol);
    }

    public async Task<Nav?> DockForSupplyConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return null;
        }

        var shouldDock = false;

        // Need fuel and fuel is available at this waypoint
        if (ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.Exchange.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
        {
            shouldDock = true;
        }
        else if (ship.Cargo.Units == 0 && currentWaypoint.Marketplace is not null && constructionWaypoint.Construction is not null)
        {
            var constructionInventoryNeeded = constructionWaypoint.Construction
                .Materials
                .Where(m => m.Required - m.Fulfilled > 0).ToList();
            var constructionInventoryNeededSymbols = constructionInventoryNeeded.Select(m => m.TradeSymbol);
            var currentWaypointExports = currentWaypoint.Marketplace.Exports.Select(e => e.Symbol);
            if (constructionInventoryNeededSymbols.Any(cins => currentWaypointExports.Contains(cins)))
            {
                shouldDock = true;
            }
        }
        else if (ship.Cargo.Units > 0 && currentWaypoint.Construction is not null && currentWaypoint.Symbol == constructionWaypoint.Symbol)
        {
            shouldDock = true;
        }

        if (!shouldDock)
        {
            return null;
        }

        return await _shipsService.DockAsync(ship.Symbol);
    }

    public async Task<(Cargo?, Cooldown?)> Extract(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Units == ship.Cargo.Capacity
            || !(currentWaypoint.Type == WaypointTypesEnum.ASTEROID.ToString()
                || currentWaypoint.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()))
        {
            return (null, null);
        }

        var surveys = (await _surveysCacheService
            .GetAsync(currentWaypoint.Symbol))
            .OrderBy(s => s.Expiration);

        var survey = surveys.FirstOrDefault();

        if (survey is not null)
        {
            var extractionSurveyResult = await _shipsService.ExtractAsync(ship.Symbol, survey);
            return (extractionSurveyResult.Cargo, extractionSurveyResult.Cooldown);
        }
        var extractionResult = await _shipsService.ExtractAsync(ship.Symbol);
        return (extractionResult.Cargo, extractionResult.Cooldown);
    }

    public async Task<(Nav?, Fuel?)> NavigateToEndWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint)
    {
        if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString()
            || ship.Cargo.Units == 0
            || currentWaypoint.Symbol == endWaypoint.Symbol)
        {
            return (null, null);
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.Key.Symbol == endWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(pathItem.Value.Item1[1].Symbol, ship.Symbol);
        return (nav, fuel);
    }

    public async Task<(Nav?, Fuel?)> NavigateToStartWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint startWaypoint)
    {
        if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString()
            || ship.Cargo.Units != 0
            || currentWaypoint.Symbol == startWaypoint.Symbol)
        {
            return (null, null);
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.Key.Symbol == startWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(pathItem.Value.Item1[1].Symbol, ship.Symbol);

        return (nav, fuel);
    }

    public async Task<(Nav?, Fuel?)> NavigateToMiningWaypoint(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString()
            || ship.Cargo.Units != 0
            || currentWaypoint.Type == WaypointTypesEnum.ASTEROID.ToString()
            || currentWaypoint.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString())
        {
            return (null, null);
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var asteroidWaypoints = system.Waypoints.Where(w =>
            w.Type == WaypointTypesEnum.ASTEROID.ToString()
            || w.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()).ToList();
        var asteroidPaths = paths.Where(p => asteroidWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol));
        var closestAsteroidPath = asteroidPaths.OrderBy(p => p.Value.Item1.Count()).FirstOrDefault();
        return await _shipsService.NavigateAsync(closestAsteroidPath.Value.Item1[1].Symbol, ship.Symbol);
    }

    public async Task<Nav?> Orbit(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || ship.Fuel.Current != ship.Fuel.Capacity)
        {
            return null;
        }

        return await _shipsService.OrbitAsync(ship.Symbol);
    }

    public async Task<Fuel?> Refuel(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Fuel.Current == ship.Fuel.Capacity
            || ship.Nav.Status != NavStatusEnum.DOCKED.ToString()
            || currentWaypoint.Marketplace is null
            || !currentWaypoint.Marketplace.TradeGoods.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()))
        {
            return null;
        }

        var fuel = await _marketplacesService.RefuelAsync(ship.Symbol);
        return fuel;
    }

    public async Task<Cargo?> Sell(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Imports.Any(i => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(i.Symbol)))
            && !currentWaypoint.Marketplace.Exchange.Any(e => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(e.Symbol)))
        {
            return null;
        }

        var cargo = ship.Cargo;
        var originalCargoCount = ship.Cargo.Inventory.Count;
        foreach (var inventory in ship.Cargo.Inventory)
        {
            if (currentWaypoint.Marketplace.Imports.Any(i => i.Symbol == inventory.Symbol))
            {
                var tradeGood = currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == inventory.Symbol);
                var units = Math.Min(tradeGood.TradeVolume, inventory.Units);
                cargo = await _marketplacesService.SellAsync(ship.Symbol, inventory.Symbol, units);
            }
            if (currentWaypoint.Marketplace.Exchange.Any(e => e.Symbol == inventory.Symbol))
            {
                cargo = await _marketplacesService.SellAsync(ship.Symbol, inventory.Symbol, inventory.Units);
            }
            await Task.Delay(1000);
        }
        return cargo;
    }

    public async Task<SupplyResult?> SupplyConstructionSite(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0
            || currentWaypoint.Construction is null)
        {
            return null;
        }

        SupplyResult supplyResult = null;
        foreach (var inventory in ship.Cargo.Inventory)
        {
            var material = currentWaypoint.Construction.Materials.SingleOrDefault(m => m.TradeSymbol == inventory.Symbol);
            if (material is null) continue;
            if (material.Fulfilled < material.Required)
            {
                supplyResult = await _constructionService.SupplyAsync(
                    currentWaypoint.Symbol,
                    ship.Symbol,
                    inventory.Symbol,
                    Math.Min(material.Required - material.Fulfilled, inventory.Units));
            }
        }
        return supplyResult;
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

    public async Task<(Nav?, Fuel?)> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0)
        {
            return (null, null);
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.Key.Symbol == endWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(pathItem.Value.Item1[1].Symbol, ship.Symbol);

        return (nav, fuel);
    }

    public async Task<(Nav?, Fuel?)> NavigateToConstructionWaypoint(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0
            || (currentWaypoint.JumpGate is not null
                && currentWaypoint.IsUnderConstruction))
        {
            return (null, null);
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var jumpGateWaypoints = system.Waypoints.Where(w => w.JumpGate is not null && w.IsUnderConstruction);
        var paths = pathDictionary.Where(p => jumpGateWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol));
        var closestPath = paths.OrderBy(p => p.Value.Item1.Count()).FirstOrDefault();

        var (nav, fuel) = await _shipsService.NavigateAsync(closestPath.Value.Item1[1].Symbol, ship.Symbol);

        return (nav, fuel);
    }


    public async Task<(Nav?, Fuel)> NavigateToMarketplaceExport(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint)
    {
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

        var waypointsReachable = pathDictionary.Select(p => p.Key.Symbol);
        var exportWaypointsReachable = system.Waypoints.Where(w => w.Marketplace is not null && w.Marketplace.Exports.Any() && waypointsReachable.Contains(w.Symbol));
        var neededConstructionInventory = constructionWaypoint.Construction.Materials.Where(m => m.Required > m.Fulfilled).ToList();

        // var neededConstructionInventoryMost = neededConstructionInventory.Select(i => (i.TradeSymbol, (double)i.Required/(double)i.Fulfilled)).OrderBy(i=> i.Item2).FirstOrDefault().TradeSymbol;

        var waypointOptions = exportWaypointsReachable.Where(ewr => neededConstructionInventory.Select(nci => nci.TradeSymbol).Any(nci => ewr.Marketplace.Exports.Select(e => e.Symbol).Contains(nci)));
        var closestWaypointPath = pathDictionary
            .Where(p => waypointOptions.Select(wo => wo.Symbol).Contains(p.Key.Symbol))
            .OrderBy(p => p.Value.Item1.Count)
            .FirstOrDefault();

        var (nav, fuel) = await _shipsService.NavigateAsync(closestWaypointPath.Value.Item1[1].Symbol, ship.Symbol);
        return (nav, fuel);
    }

    public async Task<Waypoint?> GetClosestSellingWaypoint(Ship ship, Waypoint currentWaypoint)
    {
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
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

    public async Task<(Nav nav, Fuel fuel)> NavigateToMarketplaceRandomExport(Ship ship, Waypoint currentWaypoint)
    {
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var waypointsWithinRange = paths.Select(p => p.Key);
        var waypointSymbolsWithinRange = waypointsWithinRange.Select(wwr => wwr.Symbol).ToList();

        // Navigate to any export marketplaces not mapped
        var waypointsWithinRangeWithExportsNotMapped = waypointsWithinRange.Where(wwr => wwr.Marketplace is not null && wwr.Marketplace.Exports.Any() && wwr.Marketplace.TradeGoods is null);
        var waypointSymbolsWithinRangeWithExportsNotMapped = waypointsWithinRangeWithExportsNotMapped.Select(w => w.Symbol);
        if (waypointSymbolsWithinRangeWithExportsNotMapped.Any())
        {
            var shortestPath = paths
                .Where(p => waypointSymbolsWithinRangeWithExportsNotMapped.Contains(p.Key.Symbol))
                .OrderBy(p => p.Value.Item1.Count())
                .First();
            return await _shipsService.NavigateAsync(shortestPath.Key.Symbol, ship.Symbol);
        }

        // Find closest marketplace with exports, with imports reachable, with supply at least MODERATE
        var moderateTradeGoodsWithinRange = system
            .Waypoints
            .Where(w => waypointSymbolsWithinRange.Contains(w.Symbol)
                && w.Marketplace is not null
                && w.Marketplace.TradeGoods is not null
                && w.Marketplace.TradeGoods.Any(tg =>
                    tg.Type == "EXPORT"
                    && ((SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.MODERATE)
                    && system.Waypoints.Any(w2 => waypointSymbolsWithinRange.Contains(w2.Symbol)
                        && w2.Marketplace is not null
                        && w2.Marketplace.Imports.Any(i => tg.Symbol == i.Symbol))));
        if (moderateTradeGoodsWithinRange.Any())
        {
            var pathsToModerateTradeGoods = paths.Where(p => moderateTradeGoodsWithinRange.Select(w => w.Symbol).Contains(p.Key.Symbol)).ToList();
            var shortestPath = pathsToModerateTradeGoods.OrderBy(ptmtg => ptmtg.Value.Item1.Count).ThenBy(ptmtg => ptmtg.Value.Item4).First();
            return await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship.Symbol);
        }
        else
        {
            throw new NotImplementedException("NavigateToMarketplaceRandomExport need to implement where TradeGoods are not identified.");
        }
    }

    public async Task<(Nav?, Fuel?)> NavigateToSurvey(Ship ship, Waypoint currentWaypoint)
    {
        if (currentWaypoint.Type == WaypointTypesEnum.ASTEROID.ToString()
            || currentWaypoint.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString())
        {
            return (null, null);
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var asteroidWaypoints = system.Waypoints.Where(w =>
            w.Type == WaypointTypesEnum.ASTEROID.ToString()
            || w.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()).ToList();
        var asteroidPaths = paths.Where(p => asteroidWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol));
        var closestAsteroidPath = asteroidPaths.OrderBy(p => p.Value.Item1.Count()).FirstOrDefault();
        return await _shipsService.NavigateAsync(closestAsteroidPath.Value.Item1[1].Symbol, ship.Symbol);
    }

    public async Task<Cooldown> Survey(Ship ship)
    {
        var surveys = await _surveysCacheService.GetAsync(ship.Nav.WaypointSymbol);
        if (surveys.Count() >= 5)
        {
            var timeSpan = TimeSpan.FromMinutes(1);
            return new Cooldown(ship.Symbol, (int)timeSpan.TotalSeconds, (int)timeSpan.TotalSeconds, DateTime.UtcNow.AddMinutes(timeSpan.TotalSeconds));
        }
        var surveyResult = await _shipsService.SurveyAsync(ship.Symbol);
        foreach (var survey in surveyResult.Surveys)
        {
            await _surveysCacheService.SetAsync(survey);
        }
        return surveyResult.Cooldown;
    }

    public async Task<(Nav? nav, Fuel? fuel)> NavigateToShipyard(Ship ship, Waypoint currentWaypoint)
    {
        var ships = await _shipsService.GetAsync();
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var shipToBuy = await ShipToBuy(ships, system);
        if (shipToBuy is null) return (null, null);

        var shipyards = system.Waypoints.Where(w => w.Shipyard is not null);
        var shipyardWithShip = shipyards.FirstOrDefault(s => s.Shipyard.ShipFrames.Any(sf => sf.Type == shipToBuy.ToString()));
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        if (shipyardWithShip is not null && ship.Nav.WaypointSymbol != shipyardWithShip.Symbol)
        {
            var path = paths.SingleOrDefault(p => p.Key.Symbol == shipyardWithShip.Symbol);
            var (nav, fuel) = await _shipsService.NavigateAsync(path.Value.Item1[1].Symbol, ship.Symbol);
            return (nav, fuel);
        }
        return (null, null);
    }

    public async Task<bool> PurchaseShip(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString()
            || currentWaypoint.Shipyard is null)
        {
            return false;
        }
        var ships = await _shipsService.GetAsync();
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var shipToBuy = await ShipToBuy(ships, system);
        if (shipToBuy is null 
            || !currentWaypoint.Shipyard.ShipTypes.Select(st => st.Type).Contains(shipToBuy.ToString()))
        {
            return false;
        }

        await _shipyardsService.BuyAsync(currentWaypoint.Symbol, shipToBuy.ToString());
        return true;
    }

    public static Task<ShipTypesEnum?> ShipToBuy(IEnumerable<Ship> ships, STSystem system)
    {
        var shipyards = system
            .Waypoints
            .Where(w => w.Shipyard is not null)
            .ToList();
        var shipsInSystem = ships
            .Where(s => s.Nav.SystemSymbol == system.Symbol)
            .GroupBy(s => s.Registration.Role);

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.EXCAVATOR.ToString())?.Count() ?? 0)
            > (shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.HAULER.ToString())?.Count() ?? 0))
        {
            return Task.FromResult((ShipTypesEnum?)ShipTypesEnum.SHIP_LIGHT_HAULER);
        }

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.EXCAVATOR.ToString())?.Count() ?? 0)
            == (shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.HAULER.ToString())?.Count() ?? 0)
            && (shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.EXCAVATOR.ToString())?.Count() ?? 0) < 5)
        {
            return Task.FromResult((ShipTypesEnum?)ShipTypesEnum.SHIP_MINING_DRONE);
        }

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.SURVEYOR.ToString())?.Count() ?? 0) == 0)
        {
            return Task.FromResult((ShipTypesEnum?)ShipTypesEnum.SHIP_SURVEYOR);
        }

        return Task.FromResult((ShipTypesEnum?)null);
    }
}