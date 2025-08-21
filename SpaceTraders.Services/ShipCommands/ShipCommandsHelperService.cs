using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Surveys.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
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
    private const int minimumFuel = 5;
    private readonly IShipsService _shipsService;
    private readonly IMarketplacesService _marketplacesService;
    private readonly ISystemsService _systemsService;
    private readonly IWaypointsService _waypointsService;
    private readonly IAgentsService _agentsService;
    private readonly IConstructionsService _constructionService;
    private readonly ISurveysCacheService _surveysCacheService;
    private readonly IShipyardsService _shipyardsService;
    private readonly IPathsService _pathsService;
    private readonly ITransactionsService _transactionsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    public ShipCommandsHelperService(
        IShipsService shipsService,
        IMarketplacesService marketplacesService,
        ISystemsService systemsService,
        IWaypointsService waypointsService,
        IAgentsService agentsService,
        IConstructionsService constructionsService,
        ISurveysCacheService surveysCacheService,
        IShipyardsService shipyardsService,
        IPathsService pathsService,
        ITransactionsService transactionsService,
        IShipStatusesCacheService shipStatusesCacheService)
    {
        _shipsService = shipsService;
        _marketplacesService = marketplacesService;
        _systemsService = systemsService;
        _waypointsService = waypointsService;
        _agentsService = agentsService;
        _constructionService = constructionsService;
        _surveysCacheService = surveysCacheService;
        _shipyardsService = shipyardsService;
        _pathsService = pathsService;
        _transactionsService = transactionsService;
        _shipStatusesCacheService = shipStatusesCacheService;
    }

    public async Task<PurchaseCargoResult?> PurchaseCargo(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count > 0
            || ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Exports.Any()))
        {
            return null;
        }

        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

        var agent = await _agentsService.GetAsync();

        var tradeModels = await _marketplacesService.BuildTradeModel(paths.Keys.ToList(), ship.Fuel.Capacity, ship.Fuel.Current);
        var bestTrade = _marketplacesService.GetBestTrade(tradeModels);
        if (bestTrade is null || bestTrade.ExportWaypointSymbol != currentWaypoint.Symbol) return null;
        var inventoryToBuy = currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == bestTrade.TradeSymbol);

        var newTradeModels = tradeModels;
        var newBestTrade = bestTrade;
        PurchaseCargoResult? purchaseCargoResult = null;
        while (bestTrade.TradeSymbol == newBestTrade.TradeSymbol
            && bestTrade.ExportWaypointSymbol == newBestTrade.ExportWaypointSymbol
            && ship.Cargo.Capacity - ship.Cargo.Units > 0)
        {
            var amountToBuy = Math.Min(inventoryToBuy.TradeVolume, ship.Cargo.Capacity - ship.Cargo.Units);
            if (amountToBuy * inventoryToBuy.PurchasePrice > agent.Credits)
            {
                if (ship.Cargo.Units > 0) break;
                amountToBuy = 1;
            }
            purchaseCargoResult = await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, amountToBuy);
            await Task.Delay(500);
            agent = purchaseCargoResult.Agent;
            ship = ship with { Cargo = purchaseCargoResult.Cargo };
            await _transactionsService.SetAsync(purchaseCargoResult.Transaction);

            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
            await Task.Delay(500);
            paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

            newTradeModels = await _marketplacesService.BuildTradeModel(paths.Keys.ToList(), ship.Fuel.Capacity, ship.Fuel.Current);
            newBestTrade = _marketplacesService.GetBestTrade(newTradeModels);
        }
        return purchaseCargoResult;
    }

    public async Task<PurchaseCargoResult?> BuyForConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint)
    {
        var agent = await _agentsService.GetAsync();
        if (ship.Cargo.Capacity == ship.Cargo.Units)
        {
            return null;
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);

        var marketplaceWaypoint = await GetConstructionInventoryWaypoint(
            ship,
            currentWaypoint,
            constructionWaypoint,
            system);
        if (marketplaceWaypoint.Symbol != currentWaypoint.Symbol)
        {
            return null;
        }

        var inventoryToBuy = await GetConstructionInventoryToBuy(
            ship,
            currentWaypoint,
            constructionWaypoint,
            system);

        var constructionInventory = constructionWaypoint.Construction.Materials.Single(m => m.TradeSymbol == inventoryToBuy.Symbol);

        if (inventoryToBuy is null) return null;
        var quantityToBuy = Math.Min(constructionInventory.Required - constructionInventory.Fulfilled - ship.Cargo.Units, Math.Min(inventoryToBuy.TradeVolume, (ship.Cargo.Capacity - ship.Cargo.Units)));
        if (inventoryToBuy.PurchasePrice * quantityToBuy > (agent.Credits - 200_000)) return null;
        if (quantityToBuy == 0) return null;
        var purchaseCargoResult = await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, quantityToBuy);
        return purchaseCargoResult;
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
            && currentWaypoint.Marketplace?.TradeGoods.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
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
        var shipStatuses = await _shipStatusesCacheService.GetAsync();
        var ships = shipStatuses.Select(ss => ss.Ship);
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
        else if (ship.Cargo.Units > 0)
        {
            var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
            var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

            var sellModels = _marketplacesService.BuildSellModel(paths.Keys.ToList());
            var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
            sellModels = sellModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();

            var bestTrade = _marketplacesService.GetBestSellModel(sellModels);
            if (bestTrade is null) return null;
            if (bestTrade.WaypointSymbol == currentWaypoint.Symbol) shouldDock = true;
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
            && currentWaypoint.Marketplace?.TradeGoods.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
        {
            shouldDock = true;
        }
        // Have cargo and the market wants any of it
        else
        {
            var agent = await _agentsService.GetAsync();
            // var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
            // var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
            var systems = await _systemsService.GetAsync();
            var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
            var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

            // Navigate to any export marketplaces not mapped
            var waypointsWithinRange = paths.Keys.ToList();
            var waypointsWithinRangeWithExportsNotMapped = waypointsWithinRange.Where(wwr => wwr.Marketplace is not null && wwr.Marketplace.TradeGoods is null);
            var waypointSymbolsWithinRangeWithExportsNotMapped = waypointsWithinRangeWithExportsNotMapped.Select(w => w.Symbol);
            if (!waypointSymbolsWithinRangeWithExportsNotMapped.Any())
            {
                var tradeModels = await _marketplacesService.BuildTradeModel(paths.Keys.ToList(), ship.Fuel.Capacity, ship.Fuel.Current);
                if (ship.Cargo.Units > 0)
                {
                    tradeModels = tradeModels.Where(tm => tm.TradeSymbol == ship.Cargo.Inventory.OrderByDescending(i => i.Symbol).First().Symbol).ToList();
                }
                var bestTrade = _marketplacesService.GetBestTrade(tradeModels);
                if (bestTrade is null) return null;
                if (ship.Cargo.Units > 0 && bestTrade.ImportWaypointSymbol == currentWaypoint.Symbol) shouldDock = true;
                if (ship.Cargo.Units == 0 && bestTrade.ExportWaypointSymbol == currentWaypoint.Symbol) shouldDock = true;
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
            && currentWaypoint.Marketplace?.TradeGoods.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
        {
            shouldDock = true;
        }
        else if (ship.Cargo.Units == 0 && currentWaypoint.Marketplace is not null && constructionWaypoint.Construction is not null)
        {
            var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
            var marketplaceWaypoint = await GetConstructionInventoryWaypoint(
                ship,
                currentWaypoint,
                constructionWaypoint,
                system);
            if (marketplaceWaypoint.Symbol == currentWaypoint.Symbol)
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
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString()
            || ship.Cargo.Units == ship.Cargo.Capacity
            || !(currentWaypoint.Type == WaypointTypesEnum.ASTEROID.ToString()
                || currentWaypoint.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()))
        {
            return (null, null);
        }

        ExtractionResult? extractionResult = null;
        while (extractionResult is null)
        {
            var surveys = (await _surveysCacheService
                    .GetAsync(currentWaypoint.Symbol))
                    .OrderBy(s => s.Expiration)
                    .ToDictionary(s => s, s => s.Deposits.GroupBy(d => d.Symbol));


            try
            {
                var largetsInventory = ship.Cargo.Inventory.OrderByDescending(i => i.Units).FirstOrDefault();
                var survey = surveys.Where(s => s.Value.OrderByDescending(d => d.Count()).FirstOrDefault()?.Key == largetsInventory?.Symbol).FirstOrDefault().Key;
                if (survey == default) surveys.FirstOrDefault();

                // var largetsInventory = ship.Cargo.Inventory.OrderByDescending(i => i.Units).FirstOrDefault();
                // var survey = largetsInventory is not null
                //     ? surveys.FirstOrDefault(s => s.Deposits.Any(d => d.Symbol == largetsInventory.Symbol))
                //     : surveys.FirstOrDefault();

                if (survey is not null)
                {
                    extractionResult = await _shipsService.ExtractAsync(ship.Symbol, survey);
                    return (extractionResult.Cargo, extractionResult.Cooldown);
                }
                extractionResult = await _shipsService.ExtractAsync(ship.Symbol);
                return (extractionResult.Cargo, extractionResult.Cooldown);
            }
            catch (HttpRequestException e)
            {
                if (surveys.Any())
                {
                    await _surveysCacheService.DeleteAsync(surveys.First().Key.Signature);
                }
                await Task.Delay(1000);
            }
        }
        return (null, null);
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
        var pathDictionary = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.Key.Symbol == endWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(pathItem.Value.Item1[1].Symbol, ship);
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
        var pathDictionary = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.Key.Symbol == startWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(pathItem.Value.Item1[1].Symbol, ship);

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
        var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var asteroidWaypoints = system.Waypoints.Where(w =>
            w.Type == WaypointTypesEnum.ASTEROID.ToString()
            || w.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()).ToList();
        var asteroidPaths = paths.Where(p => asteroidWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol));
        var closestAsteroidPath = asteroidPaths.OrderBy(p => p.Value.Item1.Count()).FirstOrDefault();
        return await _shipsService.NavigateAsync(closestAsteroidPath.Value.Item1[1].Symbol, ship);
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

    public async Task<RefuelResponse?> Refuel(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Fuel.Current == ship.Fuel.Capacity
            || ship.Nav.Status != NavStatusEnum.DOCKED.ToString()
            || currentWaypoint.Marketplace is null
            || currentWaypoint.Marketplace.TradeGoods?.Any(e => e.Symbol == InventoryEnum.FUEL.ToString())== false)
        {
            return null;
        }

        var refuelResponse = await _marketplacesService.RefuelAsync(ship.Symbol);
        return refuelResponse;
    }

    public async Task<SellCargoResponse?> Sell(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Imports.Any(i => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(i.Symbol)))
            && !currentWaypoint.Marketplace.Exchange.Any(e => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(e.Symbol)))
        {
            return null;
        }

        // var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        // var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        // var tradeModels = _marketplacesService.BuildTradeModel(paths.Keys.ToList());
        // var bestTrade = _marketplacesService.GetBestTrade(tradeModels);
        // if (bestTrade is null) return null;

        // var sellCargoResponse = await _marketplacesService.SellAsync(ship.Symbol, inventory.Symbol, inventory.Units);

        //var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var tradeModels = _marketplacesService.BuildSellModel(paths.Keys.ToList());
        var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
        tradeModels = tradeModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();
        var bestTrade = _marketplacesService.GetBestSellModel(tradeModels);
        if (bestTrade is null) return null;
        if (currentWaypoint.Symbol != bestTrade.WaypointSymbol) return null;
        SellCargoResponse? sellCargoResponse = null;
        while (ship.Cargo.Inventory.SingleOrDefault(i => i.Symbol == inventoryToSell)?.Units > 0)
        {
            var units = Math.Min(bestTrade.TradeVolume, ship.Cargo.Inventory.Single(i => i.Symbol == inventoryToSell).Units);
            sellCargoResponse = await _marketplacesService.SellAsync(ship.Symbol, bestTrade.TradeSymbol, units);
            ship = ship with { Cargo = sellCargoResponse.Cargo };
            await _transactionsService.SetAsync(sellCargoResponse.Transaction);
        }
        return sellCargoResponse;


        // SellCargoResponse? sellCargoResponse = null;
        // var cargo = ship.Cargo;
        // var originalCargoCount = ship.Cargo.Inventory.Count;
        // foreach (var inventory in ship.Cargo.Inventory)
        // {
        //     if (currentWaypoint.Marketplace.Imports.Any(i => i.Symbol == inventory.Symbol))
        //     {
        //         var tradeGood = currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == inventory.Symbol);
        //         var units = Math.Min(tradeGood.TradeVolume, inventory.Units);
        //         sellCargoResponse = await _marketplacesService.SellAsync(ship.Symbol, inventory.Symbol, units);
        //     }
        //     if (currentWaypoint.Marketplace.Exchange.Any(e => e.Symbol == inventory.Symbol))
        //     {
        //         sellCargoResponse = await _marketplacesService.SellAsync(ship.Symbol, inventory.Symbol, inventory.Units);
        //     }
        //     await Task.Delay(1000);
        // }
        // return sellCargoResponse;
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

    public async Task<(Nav?, Fuel?, Cooldown?)> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0
            || ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return (null, null, null);
        }

        // var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        // var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var sellModels = _marketplacesService.BuildSellModel(paths.Keys.ToList());
        var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
        sellModels = sellModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();
        var bestTrade = _marketplacesService.GetBestSellModel(sellModels);
        if (bestTrade is null)
        {
            paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, 10000, 10000);
            sellModels = _marketplacesService.BuildSellModel(paths.Keys.ToList());
            inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
            sellModels = sellModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();
            bestTrade = _marketplacesService.GetBestSellModel(sellModels);
            if (bestTrade is null) return (null, null, null);
        }
        var shortestPath = paths.Single(p => p.Key.Symbol == bestTrade.WaypointSymbol);
        
        // return await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship.Symbol);
        if (shortestPath.Value.Item1[1].SystemSymbol != ship.Nav.SystemSymbol)
        {
            var (navJump, cooldownJump) = await _shipsService.JumpAsync(shortestPath.Value.Item1[1].Symbol, ship.Symbol);
            return (navJump, ship.Fuel, cooldownJump);
        }
        if (shortestPath.Value.Item1.Count() == 1)
        {
            await _waypointsService.GetAsync(shortestPath.Key.Symbol, refresh: true);
            return (null, null, null);
        }
        var (nav, fuel) = await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship);
        return (nav, fuel, ship.Cooldown);
    }

    public static string? FindImportDestinationWithLowestSupply(Ship ship, Waypoint currentWaypoint, STSystem system)
    {
        if (ship.Cargo.Inventory.Count == 0)
        {
            return null;
        }

        var cargoSymbols = ship.Cargo.Inventory.Select(i => i.Symbol).ToHashSet();
        var pathDictionary = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

        var importWaypoint = system.Waypoints
            .Where(w =>
                w.Marketplace is not null &&
                w.Marketplace.Imports.Any(i => cargoSymbols.Contains(i.Symbol)) &&
                w.Marketplace.TradeGoods is not null
            )
            .SelectMany(w =>
                w.Marketplace.TradeGoods
                    .Where(tg => cargoSymbols.Contains(tg.Symbol))
                    .Select(tg => new
                    {
                        Waypoint = w,
                        Symbol = tg.Symbol,
                        Supply = Enum.Parse<SupplyEnum>(tg.Supply)
                    })
            )
            .OrderBy(x => x.Supply) // Prioritize by lowest supply
            .FirstOrDefault();

        return importWaypoint?.Waypoint.Symbol;
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
        var jumpGateWaypoint = system.Waypoints.FirstOrDefault(w => w.JumpGate is not null && w.IsUnderConstruction);

        var pathDictionary = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var path = pathDictionary.Single(p => p.Key.Symbol == jumpGateWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(path.Value.Item1[1].Symbol, ship);

        return (nav, fuel);
    }

    public static async Task<Waypoint?> GetConstructionInventoryWaypoint(
        Ship ship,
        Waypoint currentWaypoint,
        Waypoint constructionWaypoint,
        STSystem system)
    {
        if (constructionWaypoint.Construction is null
            || !constructionWaypoint.IsUnderConstruction)
        {
            throw new NotImplementedException();
        }

        var constructionInventoryNeeded = constructionWaypoint.Construction.Materials.Where(c => c.Fulfilled < c.Required);
        var constructionInventoryNeededSymbols = constructionInventoryNeeded.Select(cin => cin.TradeSymbol);
        var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathSymbols = paths.Select(p => p.Key.Symbol);
        var inventoryWaypoints = system.Waypoints.Where(w =>
            w.Marketplace is not null
            && w.Marketplace.Exports.Any(e => constructionInventoryNeededSymbols.Contains(e.Symbol))
            && w.Marketplace.TradeGoods is not null);
        var highestSupplyWaypoint = inventoryWaypoints
            .Select(w => new
            {
                Waypoint = w,
                HighestSupply = w.Marketplace.TradeGoods
                    .Where(tg => constructionInventoryNeededSymbols.Contains(tg.Symbol))
                    .Max(tg => Enum.Parse<SupplyEnum>(tg.Supply))
            })
            .OrderByDescending(x => x.HighestSupply)
            .ThenBy(w => w.Waypoint.Symbol)
            .ThenBy(x => paths.Single(p => p.Key.Symbol == x.Waypoint.Symbol).Value.Item1.Count())
            .FirstOrDefault()?.Waypoint;
        return highestSupplyWaypoint;
    }

    public static async Task<TradeGood?> GetConstructionInventoryToBuy(
        Ship ship,
        Waypoint currentWaypoint,
        Waypoint constructionWaypoint,
        STSystem system)
    {
        if (constructionWaypoint.Construction is null
            || !constructionWaypoint.IsUnderConstruction)
        {
            throw new NotImplementedException();
        }

        var constructionInventoryNeeded = constructionWaypoint.Construction.Materials.Where(c => c.Fulfilled < c.Required);
        var constructionInventoryNeededSymbols = constructionInventoryNeeded.Select(cin => cin.TradeSymbol);
        var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathSymbols = paths.Select(p => p.Key.Symbol);
        var inventoryWaypoints = system.Waypoints.Where(w =>
            w.Marketplace is not null
            && w.Marketplace.Exports.Any(e => constructionInventoryNeededSymbols.Contains(e.Symbol))
            && w.Marketplace.TradeGoods is not null);
        var highestSupplyWaypoint = inventoryWaypoints
            .Select(w => new
            {
                Waypoint = w,
                HighestSupply = w.Marketplace.TradeGoods
                    .Where(tg => constructionInventoryNeededSymbols.Contains(tg.Symbol))
                    .Max(tg => Enum.Parse<SupplyEnum>(tg.Supply))
            })
            .OrderByDescending(x => x.HighestSupply)
            .ThenBy(w => w.Waypoint.Symbol)
            .ThenBy(x => paths.Single(p => p.Key.Symbol == x.Waypoint.Symbol).Value.Item1.Count())
            .FirstOrDefault();

        return highestSupplyWaypoint.Waypoint.Marketplace.TradeGoods.FirstOrDefault(tg =>
            constructionInventoryNeededSymbols.Contains(tg.Symbol)
            && tg.Supply == highestSupplyWaypoint.HighestSupply.ToString());
    }

    public async Task<(Nav?, Fuel)> NavigateToMarketplaceExport(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint)
    {
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var pathDictionary = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

        var marketplaceWaypoint = await GetConstructionInventoryWaypoint(
                ship,
                currentWaypoint,
                constructionWaypoint,
                system);
        var shortestPath = pathDictionary.Single(p => p.Key.Symbol == marketplaceWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship);
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
        var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var shortestPath = paths.Where(p => sellingWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol)).ToList();
        return shortestPath.OrderBy(p => p.Value.Item1.Count).FirstOrDefault().Key;
    }

    public async Task<(Nav? nav, Fuel? fuel, Cooldown? cooldown, bool noWork)> NavigateToMarketplaceRandomExport(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString()) return (Nav: null, Fuel: null, Cooldown: null, noWork: false);

        // var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        // var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

        var waypointsWithinRange = paths.Select(p => p.Key).ToList();
        var waypointSymbolsWithinRange = waypointsWithinRange.Select(wwr => wwr.Symbol).ToList();

        // Navigate to any export marketplaces not mapped
        var waypointsWithinRangeWithExportsNotMapped = waypointsWithinRange.Where(wwr => wwr.Marketplace is not null && wwr.Marketplace.TradeGoods is null).ToList();
        var waypointSymbolsWithinRangeWithExportsNotMapped = waypointsWithinRangeWithExportsNotMapped.Select(w => w.Symbol).ToList();
        if (waypointSymbolsWithinRangeWithExportsNotMapped.Any())
        {
            var shortestPathUnmapped = paths
                .Where(p => waypointSymbolsWithinRangeWithExportsNotMapped.Contains(p.Key.Symbol))
                .OrderBy(p => p.Value.Item1.Count())
                .First();
            if (shortestPathUnmapped.Value.Item1[1].SystemSymbol != ship.Nav.SystemSymbol)
            {
                var (navJump, cooldown) = await _shipsService.JumpAsync(shortestPathUnmapped.Value.Item1[1].Symbol, ship.Symbol);
                return (navJump, ship.Fuel, cooldown, false);
            }
            if (shortestPathUnmapped.Value.Item1.Count() == 1)
            {
                await _waypointsService.GetAsync(shortestPathUnmapped.Key.Symbol, refresh: true);
                return (null, null, null, true);
            }
            var (navResponse, fuelResponse) = await _shipsService.NavigateAsync(shortestPathUnmapped.Value.Item1[1].Symbol, ship);
            return (navResponse, fuelResponse, ship.Cooldown, false);
        }

        var tradeModels = await _marketplacesService.BuildTradeModel(paths.Keys.ToList(), ship.Fuel.Capacity, ship.Fuel.Current);
        var bestTrade = _marketplacesService.GetBestTrade(tradeModels);
        if (bestTrade is null) return (null, null, null, true);
        if (bestTrade.ExportWaypointSymbol == currentWaypoint.Symbol) return (null, null, null, false);
        var shortestPath = paths.Single(p => p.Key.Symbol == bestTrade.ExportWaypointSymbol);
        if (shortestPath.Value.Item1[1].SystemSymbol != ship.Nav.SystemSymbol)
        {
            var (navJump, cooldownJump) = await _shipsService.JumpAsync(shortestPath.Value.Item1[1].Symbol, ship.Symbol);
            return (navJump, ship.Fuel, cooldownJump, false);
        }
        if (shortestPath.Value.Item1.Count() == 1)
        {
            await _waypointsService.GetAsync(shortestPath.Key.Symbol, refresh: true);
            return (null, null, null, true);
        }
        var (nav, fuel) = await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship);
        return (nav, fuel, ship.Cooldown, false);
    }

    public async Task<(Nav?, Fuel?)> NavigateToSurvey(Ship ship, Waypoint currentWaypoint)
    {
        if (currentWaypoint.Type == WaypointTypesEnum.ASTEROID.ToString()
            || currentWaypoint.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString())
        {
            return (null, null);
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var asteroidWaypoints = system.Waypoints.Where(w =>
            w.Type == WaypointTypesEnum.ASTEROID.ToString()
            || w.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()).ToList();
        var asteroidPaths = paths.Where(p => asteroidWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol));
        var closestAsteroidPath = asteroidPaths.OrderBy(p => p.Value.Item1.Count()).FirstOrDefault();
        return await _shipsService.NavigateAsync(closestAsteroidPath.Value.Item1[1].Symbol, ship);
    }

    public async Task<Cooldown> Survey(Ship ship)
    {
        var surveys = await _surveysCacheService.GetAsync(ship.Nav.WaypointSymbol);
        if (surveys.Count() >= 5)
        {
            var timeSpan = TimeSpan.FromMinutes(1);
            return new Cooldown(ship.Symbol, (int)timeSpan.TotalSeconds, (int)timeSpan.TotalSeconds, DateTime.UtcNow.Add(timeSpan));
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
        var shipStatuses = await _shipStatusesCacheService.GetAsync();
        var ships = shipStatuses.Select(ss => ss.Ship);
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var shipToBuy = await ShipToBuy(ships, system);
        if (shipToBuy is null) return (null, null);

        var shipyards = system.Waypoints.Where(w => w.Shipyard is not null);
        var unmappedShipyards = shipyards.Where(s => s.Shipyard.ShipFrames is null);
        if (unmappedShipyards.Any())
        {
            var shipyardPaths = paths.Where(p => unmappedShipyards.Select(s => s.Symbol).Contains(p.Key.Symbol));
            var closestShipyard = shipyardPaths.OrderBy(s => s.Value.Item1.Count()).ThenByDescending(s => s.Value.Item4).First();
            return await _shipsService.NavigateAsync(closestShipyard.Value.Item1[1].Symbol, ship);
        }

        var shipyardWithShip = shipyards.FirstOrDefault(s => s.Shipyard.ShipFrames.Any(sf => sf.Type == shipToBuy.ToString()));
        if (shipyardWithShip is not null && ship.Nav.WaypointSymbol != shipyardWithShip.Symbol)
        {
            var path = paths.SingleOrDefault(p => p.Key.Symbol == shipyardWithShip.Symbol);
            var (nav, fuel) = await _shipsService.NavigateAsync(path.Value.Item1[1].Symbol, ship);
            return (nav, fuel);
        }
        return (null, null);
    }

    public async Task<PurchaseShipResponse?> PurchaseShip(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString()
            || currentWaypoint.Shipyard is null)
        {
            return null;
        }
        var shipStatuses = await _shipStatusesCacheService.GetAsync();
        var ships = shipStatuses.Select(ss => ss.Ship);
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var shipToBuy = await ShipToBuy(ships, system);
        if (shipToBuy is null
            || !currentWaypoint.Shipyard.ShipTypes.Select(st => st.Type).Contains(shipToBuy.ToString()))
        {
            return null;
        }

        var response = await _shipyardsService.PurchaseShipAsync(currentWaypoint.Symbol, shipToBuy.ToString());
        return response;
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

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.EXCAVATOR.ToString())?.Count() ?? 0) < 9)
        {
            return Task.FromResult((ShipTypesEnum?)ShipTypesEnum.SHIP_MINING_DRONE);
        }

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.HAULER.ToString())?.Count() ?? 0) < 5)
        {
            return Task.FromResult((ShipTypesEnum?)ShipTypesEnum.SHIP_LIGHT_HAULER);
        }

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.SURVEYOR.ToString())?.Count() ?? 0) == 0)
        {
            return Task.FromResult((ShipTypesEnum?)ShipTypesEnum.SHIP_SURVEYOR);
        }

        return Task.FromResult((ShipTypesEnum?)null);
    }

    public async Task<PurchaseCargoResult> PurchaseFuelForRescue(Ship ship, Waypoint currentWaypoint, int fuelToBuy)
    {
        if (ship.Cargo.Inventory.Count > 0
            || ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.TradeGoods.Any(tg => tg.Symbol == InventoryEnum.FUEL.ToString())))
        {
            return null;
        }

        var inventoryToBuy = currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == InventoryEnum.FUEL.ToString());
        return await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, fuelToBuy);
    }


    public async Task<(Nav? nav, Fuel? fuel, Cooldown? cooldown)> NavigateToExplore(Ship ship, Waypoint currentWaypoint)
    {
        // var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var systems = await _systemsService.GetAsync();

        if (ship.Fuel.Current < minimumFuel)
        {
            var allSystemPaths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, 10000, 10000);
            var fuelPaths = allSystemPaths.Keys
                .Where(p => p.Marketplace?.TradeGoods?.Any(tg => tg.Symbol == InventoryEnum.FUEL.ToString()) == true)
                .ToList();
            var shortestFuelPath = fuelPaths
                .OrderBy(p => WaypointsService.CalculateDistance(currentWaypoint.X, currentWaypoint.X, p.Y, p.Y))
                .First();
            var (refuelNav, refuelFuel) = await _shipsService.NavigateAsync(shortestFuelPath.Symbol, ship);
            return (refuelNav, refuelFuel, null);
        }

        var unmappedWaypoints = systems.SelectMany(s => s.Waypoints).Where(w =>
            // w.Traits is null
            // || w.Traits.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()))
            w.Traits is null
            || (w.Marketplace is not null && w.Marketplace.TradeGoods is null)
            || w.Traits.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()))
            .Select(w => w.Symbol)
            .ToList();

        var pathDictionary = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var unmappedPaths = pathDictionary.Where(p => unmappedWaypoints.Contains(p.Key.Symbol)).ToList();
        if (!unmappedPaths.Any())
        {
            pathDictionary = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, 10000, 10000);
            unmappedPaths = pathDictionary.Where(p => unmappedWaypoints.Contains(p.Key.Symbol)).ToList();
            if (unmappedPaths.Any())
            {
                await _shipsService.SwitchShipFlightMode(ship, NavFlightModeEnum.DRIFT);
            }
            else
            {
                return (null, null, null);
            }
        }
        else
        {
            await _shipsService.SwitchShipFlightMode(ship, NavFlightModeEnum.CRUISE);
        }

        var closestUnmappedPath = unmappedPaths
            .OrderByDescending(p => WaypointsService.ExtractSystemFromWaypoint(p.Key.Symbol) == ship.Nav.SystemSymbol)
            .ThenBy(p => p.Value.Item1.Count())
            .ThenBy(p => p.Value.Item2)
            .ThenBy(p => p.Key.Symbol)
            .FirstOrDefault();

        if (WaypointsService.ExtractSystemFromWaypoint(closestUnmappedPath.Value.Item1[1].Symbol) != ship.Nav.SystemSymbol)
        {
            var (navJump, cooldown) = await _shipsService.JumpAsync(closestUnmappedPath.Value.Item1[1].Symbol, ship.Symbol);
            return (navJump, ship.Fuel, cooldown);
        }
        if (closestUnmappedPath.Value.Item1.Count() == 1)
        {
            await _waypointsService.GetAsync(closestUnmappedPath.Key.Symbol, refresh: true);
            return (null, null, null);
        }
        var (nav, fuel) = await _shipsService.NavigateAsync(closestUnmappedPath.Value.Item1[1].Symbol, ship);
        return (nav, fuel, ship.Cooldown);
    }

    public async Task<(Nav nav, Fuel fuel)> NavigateToShipToRescue(Ship ship, Waypoint currentWaypoint, Waypoint rescueShipWaypoint)
    {
        await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

        return (null, null);
    }
}