using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Surveys.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
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

public class ShipCommandsHelperService(
    IShipsService _shipsService,
    IMarketplacesService _marketplacesService,
    ISystemsService _systemsService,
    IWaypointsService _waypointsService,
    IAgentsService _agentsService,
    IConstructionsService _constructionService,
    ISurveysCacheService _surveysCacheService,
    IShipyardsService _shipyardsService,
    IPathsService _pathsService,
    ITransactionsCacheService _transactionsService,
    IShipStatusesCacheService _shipStatusesCacheService,
    ITradesService _tradesService,
    IContractsService _contractsService
) : IShipCommandsHelperService
{
    private const int minimumFuel = 5;
    private const int MAX_SURVEYS = 20;
    public async Task<PurchaseCargoResult?> PurchaseCargo(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count > 0
            || ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Exports.Any()))
        {
            return null;
        }
        var agent = await _agentsService.GetAsync();
        var tradeModels = await _tradesService.GetTradeModelsAsync();
        var goalTradeModels = tradeModels.Where(tm => tm.TradeSymbol == ship.Goal).ToList();
        var bestTrade = _tradesService.GetBestTrade(goalTradeModels);
        if (bestTrade is null || bestTrade.ExportWaypointSymbol != currentWaypoint.Symbol) return null;
        var inventoryToBuy = currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == bestTrade.TradeSymbol);

        PurchaseCargoResult? purchaseCargoResult = null;
        do
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
        } while ((int)Enum.Parse<SupplyEnum>(currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == ship.Goal).Supply) > (int)SupplyEnum.MODERATE
            && ship.Cargo.Capacity - ship.Cargo.Units > 0);

        return purchaseCargoResult;
    }

    //PurchaseCargoForContract
    public async Task<PurchaseCargoResult?> PurchaseCargoForContract(Ship ship, Waypoint currentWaypoint, string contractTradeSymbol, int inventoryAmount)
    {
        if (ship.Cargo.Inventory.Count > 0
            || ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Exports.Any()))
        {
            return null;
        }

        Agent? agent = null;
        var tradeModels = await _tradesService.GetTradeModelsAsync();
        var contractTradeModel = tradeModels
            .Where(tm => tm.TradeSymbol == contractTradeSymbol)
            .OrderByDescending(tm => (int)tm.ExportSupplyEnum)
            .OrderBy(tm => tm.ExportWaypointSymbol)
            .FirstOrDefault();

        PurchaseCargoResult? purchaseCargoResult = null;
        if (contractTradeModel is not null
            && contractTradeModel.ExportWaypointSymbol == currentWaypoint.Symbol)
        {
            do
            {
                var amountToBuy = Math.Min(inventoryAmount, ship.Cargo.Capacity - ship.Cargo.Units);
                purchaseCargoResult = await _marketplacesService.PurchaseAsync(ship.Symbol, contractTradeSymbol, amountToBuy);
                await Task.Delay(500);
                agent = purchaseCargoResult.Agent;
                ship = ship with { Cargo = purchaseCargoResult.Cargo };
                await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
                await Task.Delay(500);
            }  while (ship.Cargo.Units == inventoryAmount);
        }

        // var agent = await _agentsService.GetAsync();
        // var tradeModels = await _tradesService.GetTradeModelsAsync();
        // var goalTradeModels = tradeModels.Where(tm => tm.TradeSymbol == ship.Goal).ToList();
        // var bestTrade = _tradesService.GetBestTrade(goalTradeModels);
        // if (bestTrade is null || bestTrade.ExportWaypointSymbol != currentWaypoint.Symbol) return null;
        // var inventoryToBuy = currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == bestTrade.TradeSymbol);

        // PurchaseCargoResult? purchaseCargoResult = null;
        // do
        // {
        //     var amountToBuy = Math.Min(inventoryToBuy.TradeVolume, ship.Cargo.Capacity - ship.Cargo.Units);
        //     if (amountToBuy * inventoryToBuy.PurchasePrice > agent.Credits)
        //     {
        //         if (ship.Cargo.Units > 0) break;
        //         amountToBuy = 1;
        //     }
        //     purchaseCargoResult = await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, amountToBuy);
        //     await Task.Delay(500);
        //     agent = purchaseCargoResult.Agent;
        //     ship = ship with { Cargo = purchaseCargoResult.Cargo };
        //     await _transactionsService.SetAsync(purchaseCargoResult.Transaction);

        //     currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        //     await Task.Delay(500);
        // } while ((int)Enum.Parse<SupplyEnum>(currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == ship.Goal).Supply) > (int)SupplyEnum.MODERATE
        //     && ship.Cargo.Capacity - ship.Cargo.Units > 0);

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

        var marketplaceWaypoint = GetConstructionInventoryWaypoint(
            ship,
            currentWaypoint,
            constructionWaypoint,
            system);
        if (marketplaceWaypoint.Symbol != currentWaypoint.Symbol)
        {
            return null;
        }

        var inventoryToBuy = GetConstructionInventoryToBuy(
            ship,
            currentWaypoint,
            constructionWaypoint,
            system);
        if (inventoryToBuy is null) return null;
        var constructionInventory = constructionWaypoint.Construction?.Materials.Single(m => m.TradeSymbol == inventoryToBuy.Symbol);

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
            && currentWaypoint.Marketplace?.TradeGoods?.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
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
        var shipToBuy = await ShipToBuy(ship.Nav.SystemSymbol, ships, system);
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

            var sellModels = _tradesService.BuildSellModel(paths.Keys.ToList());
            var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
            sellModels = sellModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();

            var bestTrade = _tradesService.GetBestSellModel(sellModels);
            if (bestTrade is null) return null;
            if (bestTrade.WaypointSymbol == currentWaypoint.Symbol) shouldDock = true;
        }

        if (!shouldDock)
        {
            return null;
        }

        return await _shipsService.DockAsync(ship.Symbol);
    }

    public async Task<(Nav?, string?)> DockForBuyAndSell(Ship ship, Waypoint currentWaypoint)
    {
        string? goal = ship.Goal;
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return (null, goal);
        }

        var shouldDock = false;

        // Need fuel and fuel is available at this waypoint
        if (ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.TradeGoods?.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
        {
            shouldDock = true;
        }
        // Have cargo and the market wants any of it
        else
        {
            var tradeModels = await _tradesService.GetTradeModelsAsync();
            if (ship.Cargo.Units > 0)
            {
                tradeModels = tradeModels.Where(tm => tm.TradeSymbol == ship.Cargo.Inventory.OrderByDescending(i => i.Symbol).First().Symbol).ToList();
            }
            else if (ship.Goal is not null)
            {
                tradeModels = tradeModels.Where(tm => tm.TradeSymbol == ship.Goal).ToList();
            }
            var bestTrade = _tradesService.GetBestTrade(tradeModels);
            if (bestTrade is null) return (null, goal);
            if (ship.Cargo.Units > 0 && bestTrade.ImportWaypointSymbol == currentWaypoint.Symbol) shouldDock = true;
            if (ship.Cargo.Units == 0 && bestTrade.ExportWaypointSymbol == currentWaypoint.Symbol) 
            {
                goal = bestTrade.TradeSymbol;
                shouldDock = true;
            }
        }

        if (!shouldDock)
        {
            return (null, goal);
        }

        return (await _shipsService.DockAsync(ship.Symbol), goal);
    }

    public async Task<Nav?> DockForBuyOrFulfill(
        Ship ship, 
        Waypoint currentWaypoint, 
        string? contractWaypointSymbol, 
        string? inventorySymbol)
    {
        Nav? nav = null;
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return nav;
        }

        var shouldDock = false;

        // Need fuel and fuel is available at this waypoint
        if (ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.TradeGoods?.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
        {
            shouldDock = true;
        }
        // waypoint is the destination for the contract
        else if (currentWaypoint.Symbol == contractWaypointSymbol && ship.Cargo.Units > 0)
        {
            shouldDock = true;
        }
        // at waypoint to buy cargo
        else if (ship.Cargo.Units == 0 && ship.Goal is not null)
        {
            var tradeModels = await _tradesService.GetTradeModelsAsync();
            var contractTradeModel = tradeModels
                .Where(tm => tm.TradeSymbol == inventorySymbol)
                .OrderByDescending(tm => (int)tm.ExportSupplyEnum)
                .OrderBy(tm => tm.ExportWaypointSymbol)
                .FirstOrDefault();
            if (contractTradeModel is not null
                && contractTradeModel.ExportWaypointSymbol == currentWaypoint.Symbol)
            {
                shouldDock = true;
            }        
        }
        // at faction waypoint for new contract
        else if (ship.Cargo.Units == 0 && ship.Goal is null)
        {
            var agent = await _agentsService.GetAsync();
            shouldDock = agent.Headquarters == currentWaypoint.Symbol;
        }

        if (!shouldDock)
        {
            return nav;
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
            && currentWaypoint.Marketplace?.TradeGoods?.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
        {
            shouldDock = true;
        }
        else if (ship.Cargo.Units == 0 && currentWaypoint.Marketplace is not null && constructionWaypoint.Construction is not null)
        {
            var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
            var marketplaceWaypoint = GetConstructionInventoryWaypoint(
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
            // var surveys = (await _surveysCacheService
            //         .GetAsync(currentWaypoint.Symbol))
            //         .OrderBy(s => s.Expiration)
            //         .ToDictionary(s => s, s => s.Deposits.GroupBy(d => d.Symbol));

            var surveys = await _surveysCacheService.GetAsync(currentWaypoint.Symbol);
            
            try
            {
                var singleResourceSurveys = 
                    surveys
                        .Where(s => s.Deposits.Select(d => d.Symbol).Distinct().Count() == 1)
                        .Select(survey => (survey, survey.Deposits.First().Symbol, survey.Size))
                        .ToList();
                var largestInventory = ship.Cargo.Inventory.OrderByDescending(i => i.Units).FirstOrDefault()?.Symbol;
                Survey? survey = null;

                if (largestInventory is not null 
                    && singleResourceSurveys is not null
                    && singleResourceSurveys.Any(srs => srs.Symbol == largestInventory))
                {
                    survey = singleResourceSurveys
                        .Where(srs => largestInventory == srs.Symbol)
                        .OrderByDescending(srs => Enum.Parse<SurveySizeEnum>(srs.Size))
                        .First()
                        .survey;
                }
                
                if (survey is null
                    && largestInventory is not null)
                {
                    survey = surveys
                        .OrderByDescending(s => s.Deposits.Count(d => d.Symbol == largestInventory))
                        .ThenByDescending(s => Enum.Parse<SurveySizeEnum>(s.Size))
                        .FirstOrDefault();
                }
                
                survey ??= surveys
                        .OrderByDescending(srs => Enum.Parse<SurveySizeEnum>(srs.Size))
                        .FirstOrDefault();

                if (survey is not null)
                {
                    extractionResult = await _shipsService.ExtractAsync(ship.Symbol, survey);
                    return (extractionResult.Cargo, extractionResult.Cooldown);
                }
                extractionResult = await _shipsService.ExtractAsync(ship.Symbol);
                return (extractionResult.Cargo, extractionResult.Cooldown);
            }
            catch (HttpRequestException)
            {
                if (surveys.Any())
                {
                    await _surveysCacheService.DeleteAsync(surveys.First().Signature);
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
            || currentWaypoint.Marketplace.TradeGoods?.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == false)
        {
            return null;
        }

        var refuelResponse = await _marketplacesService.RefuelAsync(ship.Symbol);
        return refuelResponse;
    }

    public bool IsFuelNeeded(Ship ship)
    {
        return ship.Fuel.Current != ship.Fuel.Capacity;
    }
    
    public bool IsWaypointFuelAvailable(Waypoint waypoint)
    {
        return waypoint
            .Marketplace?
            .TradeGoods?
            .Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true;
    }

    public bool IsAnyItemToSellAtCurrentWaypoint(Ship ship, Waypoint waypoint)
    {
        return ship.Cargo.Inventory.Count > 0
            && (waypoint.Marketplace?.Imports.Any(i => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(i.Symbol)) == true
            || waypoint.Marketplace?.Exchange.Any(e => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(e.Symbol)) == true);
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

        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var reachableWaypoints = waypoints.Where(w => paths.Keys.Contains(w.Symbol)).ToList();
        var tradeModels = _tradesService.BuildSellModel(reachableWaypoints);
        var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
        tradeModels = tradeModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();
        // var bestTrade = _tradesService.GetBestSellModel(tradeModels);
        // if (bestTrade is null && !tradeModels.Any(tm => tm.WaypointSymbol == ship.Nav.WaypointSymbol)) return null;
        var currentTrade = tradeModels.SingleOrDefault(tm => tm.WaypointSymbol == ship.Nav.WaypointSymbol);
        if (currentTrade is null) return null;
        SellCargoResponse? sellCargoResponse = null;
        while (ship.Cargo.Inventory.SingleOrDefault(i => i.Symbol == inventoryToSell)?.Units > 0)
        {
            var units = Math.Min(currentTrade.TradeVolume, ship.Cargo.Inventory.Single(i => i.Symbol == inventoryToSell).Units);
            sellCargoResponse = await _marketplacesService.SellAsync(ship.Symbol, currentTrade.TradeSymbol, units);
            ship = ship with { Cargo = sellCargoResponse.Cargo };
            await _transactionsService.SetAsync(sellCargoResponse.Transaction);
        }
        return sellCargoResponse;
    }

    public async Task<SupplyResult?> SupplyConstructionSite(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0
            || currentWaypoint.Construction is null)
        {
            return null;
        }

        SupplyResult? supplyResult = null;
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

    public async Task<Cargo?> Jettison(Ship ship)
    {
        if (ship.Cargo.Inventory.Count == 0)
        {
            return null;
        }

        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var waypoints = system.Waypoints.ToList();
        var currentWaypoint = waypoints.Single(w => w.Symbol == ship.Nav.WaypointSymbol);
        var reachablePaths = PathsService.BuildWaypointPath(waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        // var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        // var reachableWaypoints = waypoints.Where(w => paths.ContainsKey(w.Symbol)).ToList();
        var sellModels = _tradesService.BuildSellModel(waypoints);
        var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
        sellModels = sellModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();
        var bestTrade = _tradesService.GetBestSellModel(sellModels);
     
        Cargo? cargo = null;
        if (bestTrade is null)
        {
            foreach (var inventory in ship.Cargo.Inventory)
            {
                cargo = await _shipsService.JettisonAsync(ship.Symbol, inventory.Symbol, inventory.Units);
            }
        }
        
        return cargo;
    }

    public async Task<(Nav?, Fuel?, Cooldown?)> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0
            || ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return (null, null, null);
        }

        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var reachableWaypoints = waypoints.Where(w => paths.ContainsKey(w.Symbol)).ToList();
        var sellModels = _tradesService.BuildSellModel(reachableWaypoints);
        var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
        sellModels = sellModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();
        var bestTrade = _tradesService.GetBestSellModel(sellModels);
        var shortestPath = paths.Single(p => p.Key == bestTrade.WaypointSymbol);
        
        if (WaypointsService.ExtractSystemFromWaypoint(shortestPath.Value.Item1[1]) != ship.Nav.SystemSymbol)
        {
            var (navJump, cooldownJump) = await _shipsService.JumpAsync(shortestPath.Value.Item1[1], ship.Symbol);
            return (navJump, ship.Fuel, cooldownJump);
        }
        if (shortestPath.Value.Item1.Count() == 1)
        {
            await _waypointsService.GetAsync(shortestPath.Key, refresh: true);
            return (null, null, null);
        }
        var (nav, fuel) = await _shipsService.NavigateAsync(shortestPath.Value.Item1[1], ship);
        return (nav, fuel, ship.Cooldown);
    }

    public async Task<(Nav?, Fuel?, Cooldown?)> NavigateToFulfillContract(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0
            || ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return (null, null, null);
        }

        var agent = await _agentsService.GetAsync();
        var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, 10000, 10000);
        var path = paths.Single(p => p.Key == agent.Headquarters);
        
        if (WaypointsService.ExtractSystemFromWaypoint(path.Value.Item1[1]) != ship.Nav.SystemSymbol)
        {
            var (navJump, cooldownJump) = await _shipsService.JumpAsync(path.Value.Item1[1], ship.Symbol);
            return (navJump, ship.Fuel, cooldownJump);
        }
        if (path.Value.Item1.Count() == 1)
        {
            await _waypointsService.GetAsync(path.Key, refresh: true);
            return (null, null, null);
        }
        var (nav, fuel) = await _shipsService.NavigateAsync(path.Value.Item1[1], ship);
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
        var jumpGateWaypoint = system.Waypoints.First(w => w.JumpGate is not null && w.IsUnderConstruction);

        var pathDictionary = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var path = pathDictionary.Single(p => p.Key.Symbol == jumpGateWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(path.Value.Item1[1].Symbol, ship);

        return (nav, fuel);
    }

    public static Waypoint? GetConstructionInventoryWaypoint(
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

    public static TradeGood? GetConstructionInventoryToBuy(
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

        return highestSupplyWaypoint?.Waypoint.Marketplace?.TradeGoods?.FirstOrDefault(tg =>
            constructionInventoryNeededSymbols.Contains(tg.Symbol)
            && tg.Supply == highestSupplyWaypoint.HighestSupply.ToString());
    }

    public async Task<(Nav?, Fuel?)> NavigateToMarketplaceExport(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint)
    {
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var pathDictionary = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

        var marketplaceWaypoint = GetConstructionInventoryWaypoint(
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

    public async Task<(Nav? nav, Fuel? fuel, Cooldown? cooldown, bool noWork, string? goal)> NavigateToMarketplaceRandomExport(
        Ship ship, 
        Waypoint currentWaypoint,
        IEnumerable<string> otherShipGoalSymbols)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString()) return (null, null, null, noWork: false, goal: null);
        string? goal = ship.Goal;

        var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

        var tradeModels = await _tradesService.GetTradeModelsAsync();
        var availableTradeModels = tradeModels.Where(tm => !otherShipGoalSymbols.Contains(tm.TradeSymbol)).ToList();
        if (goal is not null)
        {
            availableTradeModels = tradeModels.Where(tm => tm.TradeSymbol == ship.Goal).ToList();
        }
        var bestTrade = _tradesService.GetBestTrade(availableTradeModels);
        if (bestTrade is null) return (null, null, null, noWork: true, goal: null);
        goal = bestTrade.TradeSymbol;
        if (bestTrade.ExportWaypointSymbol == currentWaypoint.Symbol) return (null, null, null, noWork: false, goal);

        var shortestPath = paths.Single(p => p.Key == bestTrade.ExportWaypointSymbol);
        if (WaypointsService.ExtractSystemFromWaypoint(shortestPath.Value.Item1[1]) != ship.Nav.SystemSymbol)
        {
            var (navJump, cooldownJump) = await _shipsService.JumpAsync(shortestPath.Value.Item1[1], ship.Symbol);
            return (navJump, ship.Fuel, cooldownJump, noWork: false, goal);
        }
        if (shortestPath.Value.Item1.Count() == 1)
        {
            await _waypointsService.GetAsync(shortestPath.Key, refresh: true);
            return (null, null, null, true, null);
        }
        var (nav, fuel) = await _shipsService.NavigateAsync(shortestPath.Value.Item1[1], ship);
        return (nav, fuel, ship.Cooldown, false, goal);
    }

    public async Task<(Nav? nav, Fuel? fuel, Cooldown? cooldown, bool noWork, string? goal)> NavigateToMarketplaceExportForContract(
        Ship ship, 
        Waypoint currentWaypoint,
        string inventorySymbol)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString()) return (null, null, null, noWork: false, goal: null);
        string? goal = ship.Goal;

        var paths = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var tradeModels = await _tradesService.GetTradeModelsAsync();
        var contractTradeModel = tradeModels
            .Where(tm => tm.TradeSymbol == inventorySymbol)
            .OrderByDescending(tm => (int)tm.ExportSupplyEnum)
            .OrderBy(tm => tm.ExportWaypointSymbol)
            .FirstOrDefault();

        if (contractTradeModel is null) return (null, null, null, noWork: true, goal: null);
        if (contractTradeModel.ExportWaypointSymbol == currentWaypoint.Symbol) return (null, null, null, noWork: false, goal);

        var shortestPath = paths.Single(p => p.Key == contractTradeModel.ExportWaypointSymbol);
        if (WaypointsService.ExtractSystemFromWaypoint(shortestPath.Value.Item1[1]) != ship.Nav.SystemSymbol)
        {
            var (navJump, cooldownJump) = await _shipsService.JumpAsync(shortestPath.Value.Item1[1], ship.Symbol);
            return (navJump, ship.Fuel, cooldownJump, noWork: false, goal);
        }
        if (shortestPath.Value.Item1.Count() == 1)
        {
            await _waypointsService.GetAsync(shortestPath.Key, refresh: true);
            return (null, null, null, true, null);
        }
        var (nav, fuel) = await _shipsService.NavigateAsync(shortestPath.Value.Item1[1], ship);
        return (nav, fuel, ship.Cooldown, false, goal);
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
        var asteroidPaths = paths.Where(p => asteroidWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol)).ToList();
        var closestAsteroidPath = asteroidPaths.OrderBy(p => p.Value.Item1.Count()).FirstOrDefault();
        if (asteroidPaths.Count == 0)
        {
            paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, 10000, 10000);
            asteroidWaypoints = system.Waypoints.Where(w =>
                w.Type == WaypointTypesEnum.ASTEROID.ToString()
                || w.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()).ToList();
            asteroidPaths = paths.Where(p => asteroidWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol)).ToList();
            closestAsteroidPath = asteroidPaths.OrderBy(p => p.Value.Item1.Count()).FirstOrDefault();
        }
        return await _shipsService.NavigateAsync(closestAsteroidPath.Value.Item1[1].Symbol, ship);
    }

    public async Task<Cooldown> Survey(Ship ship)
    {
        var surveys = await _surveysCacheService.GetAsync(ship.Nav.WaypointSymbol);
        if (surveys.Count() >= MAX_SURVEYS)
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
        var shipToBuy = await ShipToBuy(ship.Nav.SystemSymbol, ships, system);
        if (shipToBuy is null) return (null, null);

        var shipyards = system.Waypoints.Where(w => w.Shipyard is not null);
        var unmappedShipyards = shipyards.Where(s => s.Shipyard?.ShipFrames is null);
        if (unmappedShipyards.Any())
        {
            var shipyardPaths = paths.Where(p => unmappedShipyards.Select(s => s.Symbol).Contains(p.Key.Symbol));
            var closestShipyard = shipyardPaths.OrderBy(s => s.Value.Item1.Count()).ThenByDescending(s => s.Value.Item4).First();
            return await _shipsService.NavigateAsync(closestShipyard.Value.Item1[1].Symbol, ship);
        }

        var shipyardWithShip = shipyards.FirstOrDefault(s => s.Shipyard?.ShipFrames.Any(sf => sf.Type == shipToBuy.ToString()) == true);
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
        var shipToBuy = await ShipToBuy(ship.Nav.SystemSymbol, ships, system);
        if (shipToBuy is null
            || !currentWaypoint.Shipyard.ShipTypes.Select(st => st.Type).Contains(shipToBuy.ToString()))
        {
            return null;
        }
        var response = await _shipyardsService.PurchaseShipAsync(currentWaypoint.Symbol, shipToBuy.Value.ToString());
        return response;
    }

    public async Task<ShipTypesEnum?> ShipToBuy(string currentSystem, IEnumerable<Ship> ships, STSystem system)
    {
        var systems = await _systemsService.GetAsync();
        var reachableSystems = SystemsService.Traverse(systems, currentSystem);
        var waypoints = reachableSystems.SelectMany(w => w.Waypoints).ToList();
        var shipyards = 
            waypoints
            .Where(w => w.Shipyard is not null)
            .ToList();
        var shipsInSystem = ships
            .GroupBy(s => s.Registration.Role);

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.SURVEYOR.ToString())?.Count() ?? 0) == 0)
        {
            return ShipTypesEnum.SHIP_SURVEYOR;
        }

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.EXCAVATOR.ToString())?.Count() ?? 0) < 9)
        {
            return ShipTypesEnum.SHIP_MINING_DRONE;
        }

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.HAULER.ToString())?.Count() ?? 0) < 5)
        {
            return ShipTypesEnum.SHIP_LIGHT_HAULER;
        }

        if ((shipsInSystem.SingleOrDefault(sin => sin.Key == ShipRegistrationRolesEnum.SURVEYOR.ToString())?.Count() ?? 0) == 0)
        {
            return ShipTypesEnum.SHIP_LIGHT_SHUTTLE;
        }

        return (ShipTypesEnum?)null;
    }

    public async Task<PurchaseCargoResult?> PurchaseFuelForRescue(Ship ship, Waypoint currentWaypoint, int fuelToBuy)
    {
        if (ship.Cargo.Inventory.Count > 0
            || ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || currentWaypoint.Marketplace?.TradeGoods is null
            || (!currentWaypoint.Marketplace.TradeGoods.Any(tg => tg.Symbol == InventoryEnum.FUEL.ToString())))
        {
            return null;
        }

        var inventoryToBuy = currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == InventoryEnum.FUEL.ToString());
        return await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, fuelToBuy);
    }

    public async Task<(Nav? nav, Fuel? fuel, Cooldown? cooldown, string? goal)> NavigateToExplore(
        Ship ship, 
        Waypoint currentWaypoint,
        List<string> otherShipGoals)
    {
        var systems = await _systemsService.GetAsync();
        var reachableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = reachableSystems.SelectMany(s => s.Waypoints).ToList();

        if (ship.Fuel.Current < minimumFuel)
        {
            var paths = await _pathsService.BuildSystemPathWithCost(waypoints, currentWaypoint, 10000, 10000);
            var reachableWaypoints = waypoints.Where(w => paths.ContainsKey(w.Symbol)).ToList();
            var fuelPaths = reachableWaypoints
                .Where(p => ship.Nav.SystemSymbol == WaypointsService.ExtractSystemFromWaypoint(p.Symbol) && 
                    p.Marketplace?.TradeGoods?.Any(tg => tg.Symbol == InventoryEnum.FUEL.ToString()) == true)
                .ToList();
            var shortestFuelPath = fuelPaths
                .OrderBy(p => WaypointsService.CalculateDistance(currentWaypoint.X, currentWaypoint.X, p.Y, p.Y))
                .First();
            var (refuelNav, refuelFuel) = await _shipsService.NavigateAsync(shortestFuelPath.Symbol, ship);
            return (refuelNav, refuelFuel, null, ship.Goal);
        }

        if (ship.Nav.WaypointSymbol == ship.Goal)
        {
            ship = ship with { Goal = null };
        }

        if (ship.Goal is not null)
        {
            var paths = await _pathsService.BuildSystemPathWithCost(waypoints, currentWaypoint, 10000, 10000);
            var path = paths.Single(p => p.Key == ship.Goal);
            var (refuelNav, refuelFuel) = await _shipsService.NavigateAsync(path.Value.Item1[1], ship);
        }

        var unmappedWaypoints = waypoints
            .Where(w =>
                !WaypointsService.IsMarketplaceVisited(w)
                && !otherShipGoals.Contains(w.Symbol))
            .Select(w => w.Symbol)
            .ToList();

        if (unmappedWaypoints.Count == 0 && waypoints.Any(w => w.JumpGate is not null && !w.IsUnderConstruction))
        {
            unmappedWaypoints = waypoints
                .Where(w =>
                    !WaypointsService.IsVisited(w)
                    && !otherShipGoals.Contains(w.Symbol))
                .Select(w => w.Symbol)
                .ToList();
        }

        var pathDictionary = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var unmappedPaths = pathDictionary.Where(p => unmappedWaypoints.Contains(p.Key)).ToList();
        if (unmappedPaths.Count == 0)
        {
            pathDictionary = await _pathsService.BuildSystemPath(currentWaypoint.Symbol, 10000, 10000);
            unmappedPaths = pathDictionary.Where(p => unmappedWaypoints.Contains(p.Key)).ToList();
            if (unmappedPaths.Count == 0)
            {
                return (null, null, null, null);
            }
        }
        // else
        // {
        //     await _shipsService.SwitchShipFlightMode(ship, NavFlightModeEnum.CRUISE);
        // }

        var closestUnmappedPath = unmappedPaths
            .OrderByDescending(p => WaypointsService.ExtractSystemFromWaypoint(p.Key) == ship.Nav.SystemSymbol)
            .ThenBy(p => p.Value.Item1.Count())
            .ThenBy(p => p.Value.Item2)
            .ThenBy(p => p.Key)
            .FirstOrDefault();

        var goal = closestUnmappedPath.Key;

        if (WaypointsService.ExtractSystemFromWaypoint(closestUnmappedPath.Value.Item1[1]) != ship.Nav.SystemSymbol)
        {
            var (navJump, cooldown) = await _shipsService.JumpAsync(closestUnmappedPath.Value.Item1[1], ship.Symbol);
            return (navJump, ship.Fuel, cooldown, goal);
        }
        var (nav, fuel) = await _shipsService.NavigateAsync(closestUnmappedPath.Value.Item1[1], ship);
        return (nav, fuel, ship.Cooldown, goal);
    }

    public async Task<(Nav? nav, Fuel? fuel)> NavigateToShipToRescue(Ship ship, Waypoint currentWaypoint, Waypoint rescueShipWaypoint)
    {
        await _pathsService.BuildSystemPath(currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

        return (null, null);
    }

    public async Task<Nav?> Dock(Ship ship, Waypoint waypoint)
    {
        return await _shipsService.DockAsync(ship.Symbol);
    }

    public async Task<(STContract?, Cargo?, Agent?)> FulfillContract(Ship ship, STContract contract)
    {
        Cargo? cargo = null;
        Agent? agent = await _agentsService.GetAsync();
        if (ship.Cargo.Inventory.Count > 0
            && contract.Terms.Deliver[0].TradeSymbol == ship.Cargo.Inventory[0].Symbol
            && contract.Terms.Deliver[0].UnitsRequired == ship.Cargo.Inventory[0].Units
            && ship.Nav.WaypointSymbol == agent.Headquarters)
        {
            var contractDeliverResult = await _contractsService.DeliverAsync(contract.Id, ship.Symbol, ship.Cargo.Inventory[0].Symbol, ship.Cargo.Inventory[0].Units);
            contract = contractDeliverResult.Contract;
            cargo = contractDeliverResult.Cargo;

            var contractFulfillResult = await _contractsService.FulfillAsync(contract.Id);
            contract = contractFulfillResult.Contract;
            agent = contractFulfillResult.Agent;

            var contractNegotiateResult = await _contractsService.NegotiateAsync(ship.Symbol);
            contract = contractNegotiateResult.Contract;
        }
        return (contract, cargo, agent);
    }
}