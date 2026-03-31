using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Models.Results;
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
    ITradesService _tradesService
) : IShipCommandsHelperService
{
    private const int minimumFuel = 5;
    private const int MAX_SURVEYS = 20;

    public async Task<PurchaseCargoResult?> PurchaseCargo(Ship ship, Waypoint currentWaypoint, string tradeSymbol, int maxQuantity = int.MaxValue)
    {
        if (ship.Cargo.Inventory.Count > 0
            || ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || currentWaypoint.Marketplace is null
            || !(currentWaypoint.Marketplace.Exports.Any() || currentWaypoint.Marketplace.Exchange.Any()))
        {
            return null;
        }
        var agent = await _agentsService.GetAsync();
        var tradeGood = currentWaypoint.Marketplace?.TradeGoods?.Single(tg => tg.Symbol == tradeSymbol);
        SupplyEnum? newTradeGoodSupply;

        PurchaseCargoResult? purchaseCargoResult = null;
        do
        {
            var amountToBuy = Math.Min(Math.Min(tradeGood.TradeVolume, ship.Cargo.Capacity - ship.Cargo.Units), maxQuantity - ship.Cargo.Units);
            if (amountToBuy * tradeGood.PurchasePrice > agent.Credits)
            {
                if (ship.Cargo.Units > 0) break;
                amountToBuy = 1;
            }
            purchaseCargoResult = await _marketplacesService.PurchaseAsync(ship.Symbol, tradeGood.Symbol, amountToBuy);
            agent = purchaseCargoResult.Agent;
            ship = ship with { Cargo = purchaseCargoResult.Cargo };
            await _transactionsService.SetAsync(purchaseCargoResult.Transaction);

            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
            tradeGood = currentWaypoint.Marketplace?.TradeGoods?.SingleOrDefault(tg => tg.Symbol == tradeGood.Symbol);
            if (tradeGood is null) 
            {
                break;
            }
            newTradeGoodSupply = Enum.Parse<SupplyEnum>(tradeGood.Supply);
        } while ((int)newTradeGoodSupply > (int)SupplyEnum.MODERATE
            && ship.Cargo.Capacity - ship.Cargo.Units > 0 && ship.Cargo.Units < maxQuantity);

        return purchaseCargoResult;
    }

    public async Task<PurchaseCargoResult?> PurchaseCargoForContract(Ship ship, Waypoint currentWaypoint, string contractTradeSymbol, int inventoryAmount)
    {
        if (ship.Cargo.Inventory.Count > 0
            || ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Exports.Any()
            && !currentWaypoint.Marketplace.Exchange.Any()))
        {
            return null;
        }

        Agent? agent = null;
        if (!currentWaypoint.Marketplace?.Exports.Any(e => e.Symbol == contractTradeSymbol) == true
            && !currentWaypoint.Marketplace?.Exchange.Any(e => e.Symbol == contractTradeSymbol) == true
            && !currentWaypoint.Marketplace?.Imports.Any(e => e.Symbol == contractTradeSymbol) == true)
        {
            return null;
        }
        var contractTradeModelTradeVolume = currentWaypoint.Marketplace?.TradeGoods?.Single(tg => tg.Symbol == contractTradeSymbol).TradeVolume;

        PurchaseCargoResult? purchaseCargoResult = null;
        do
        {
            var amountToBuy = Math.Min(Math.Min(inventoryAmount - ship.Cargo.Units, contractTradeModelTradeVolume.Value), ship.Cargo.Capacity - ship.Cargo.Units);
            purchaseCargoResult = await _marketplacesService.PurchaseAsync(ship.Symbol, contractTradeSymbol, amountToBuy);
            //await Task.Delay(500);
            agent = purchaseCargoResult.Agent;
            ship = ship with { Cargo = purchaseCargoResult.Cargo };
            await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
            //await Task.Delay(500);
        }  while (ship.Cargo.Units != inventoryAmount && ship.Cargo.Units < ship.Cargo.Capacity);

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
        if (marketplaceWaypoint is null) throw new SpaceTraderResultException("No marketplace for construction materials found.");
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
        if (constructionInventory is null) throw new SpaceTraderResultException("Could not find construction inventory.");

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
        var (shipyardWaypointSymbol, shipToBuy) = await ShipToBuy(ships);
        if (shipToBuy is null || shipyardWaypointSymbol != ship.Nav.WaypointSymbol) return null;

        // Need fuel and fuel is available at this waypoint
        if ((ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.TradeGoods?.Any(e => e.Symbol == InventoryEnum.FUEL.ToString()) == true)
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
            // var paths = PathsService.BuildSystemPathWithCost(system.Waypoints.ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);

            // var pathWaypoints = system.Waypoints.Where(w => paths.Select(p => p.WaypointSymbol).Contains(w.Symbol)).ToList();
            var sellModels = await _tradesService.GetSellModelsAsyncWithBurn2([ship.Nav.SystemSymbol], ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
            var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
            sellModels = sellModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();

            var bestTrade = sellModels.OrderByDescending(sm => sm.NavigationFactor).FirstOrDefault();
            if (bestTrade is null) return null;
            if (bestTrade.WaypointSymbol == currentWaypoint.Symbol) shouldDock = true;
        }

        if (!shouldDock)
        {
            return null;
        }

        return await _shipsService.DockAsync(ship.Symbol);
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
        else if (contractWaypointSymbol is null || currentWaypoint.Symbol == contractWaypointSymbol && ship.Cargo.Units > 0)
        {
            shouldDock = true;
        }
        // at waypoint to buy cargo
        else if (ship.Cargo.Units == 0 && ship.Goal is not null)
        {
            if (currentWaypoint.Marketplace?.Exports.Any(e => e.Symbol == inventorySymbol) == true
                || currentWaypoint.Marketplace?.Exchange.Any(e => e.Symbol == inventorySymbol) == true
                || currentWaypoint.Marketplace?.Imports.Any(e => e.Symbol == inventorySymbol) == true)
                shouldDock = true;     
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
            if (marketplaceWaypoint?.Symbol == currentWaypoint.Symbol)
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
            || !(currentWaypoint.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()))
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
            }
        }
        return (null, null);
    }

    public async Task<(Cargo?, Cooldown?)> Siphon(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString()
            || ship.Cargo.Units == ship.Cargo.Capacity
            || currentWaypoint.Type != WaypointTypesEnum.GAS_GIANT.ToString())
        {
            return (null, null);
        }

        var siphonResult = await _shipsService.SiphonAsync(ship.Symbol);
        return (siphonResult.Cargo, siphonResult.Cooldown);
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
        var pathDictionary = PathsService.BuildSystemPathWithCost(system.Waypoints.ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.WaypointSymbol == endWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(pathItem.PathWaypointSymbols[1], ship);
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
        var pathDictionary = PathsService.BuildSystemPathWithCost(system.Waypoints.ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathItem = pathDictionary.SingleOrDefault(p => p.WaypointSymbol == startWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(pathItem.PathWaypointSymbols[1], ship);

        return (nav, fuel);
    }

    public async Task<(Nav?, Fuel?)> NavigateToMiningWaypoint(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString()
            || ship.Cargo.Units != 0
            || currentWaypoint.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString())
        {
            return (null, null);
        }

        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        var paths = PathsService.BuildSystemPathWithCost(waypoints, currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var asteroidWaypoints = waypoints.Where(w => w.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()).ToList();
        var asteroidPaths = paths.Where(p => asteroidWaypoints.Select(w => w.Symbol).Contains(p.WaypointSymbol));
        var closestAsteroidPath = asteroidPaths.OrderBy(p => p.TimeCost).FirstOrDefault();
        return await _shipsService.NavigateAsync(closestAsteroidPath.PathWaypointSymbols[1], ship);
    }

    public async Task<(Nav?, Fuel?, Cooldown)> NavigateToSiphonWaypoint(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString()
            || ship.Cargo.Units != 0
            || currentWaypoint.Type == WaypointTypesEnum.GAS_GIANT.ToString())
        {
            return (null, null, ship.Cooldown);
        }

        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        var asteroidWaypoints = waypoints.Where(w =>
            w.Type == WaypointTypesEnum.GAS_GIANT.ToString()).ToList();

        var paths = PathsService.BuildSystemPathWithCost(waypoints.ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var asteroidPaths = paths.Where(p => asteroidWaypoints.Any(w => w.Symbol == p.WaypointSymbol));
        var closestAsteroidPath = asteroidPaths.OrderBy(p => p.PathWaypointSymbols.Count()).FirstOrDefault();

        return await NavigateHelper(ship, closestAsteroidPath.PathWaypointSymbols[1]);
    }

    public async Task<(Nav, Fuel, Cooldown)> NavigateHelper(Ship ship, string waypointSymbol)
    {
        Nav nav = ship.Nav;
        Fuel? fuel = ship.Fuel;
        Cooldown cooldown = ship.Cooldown;

        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol, int.MaxValue);
        var paths = await _pathsService.BuildSystemPathWithCostWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var path = paths.Single(p => p.WaypointSymbol == waypointSymbol);
        if (path.PathWaypoints.Count() == 1) return (nav, fuel, cooldown);
        var nextHop = path.PathWaypoints[1];

        if (WaypointsService.ExtractSystemFromWaypoint(nextHop.WaypointSymbol) != WaypointsService.ExtractSystemFromWaypoint(ship.Nav.WaypointSymbol))
        {
            (nav, cooldown) = await _shipsService.JumpAsync(nextHop.WaypointSymbol, ship.Symbol);
            return (nav, fuel, cooldown);
        }
        nav = await _shipsService.NavToggleAsync(ship, nextHop.FlightModeEnum);
        (nav, fuel) = await _shipsService.NavigateAsync(nextHop.WaypointSymbol, ship);
        return (nav, fuel, cooldown);
    }

    public async Task<Nav?> Orbit(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString()
            || (ship.Fuel.Current != ship.Fuel.Capacity
                && currentWaypoint.Marketplace?.TradeGoods?.Any(tg => tg.Symbol == TradeSymbolsEnum.FUEL.ToString())== true))
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
        if (ship.Cargo.Units == 0
            || currentWaypoint.Marketplace is null
            || !(currentWaypoint.Marketplace.Imports.Any(i => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(i.Symbol))
                || currentWaypoint.Marketplace.Exchange.Any(e => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(e.Symbol))))
        {
            return null;
        }

        var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
        var tradeGood = currentWaypoint.Marketplace?.TradeGoods?.SingleOrDefault(tg => tg.Symbol == inventoryToSell);
        if (tradeGood is null) return null;
        
        SellCargoResponse? sellCargoResponse = null;
        while (ship.Cargo.Inventory.SingleOrDefault(i => i.Symbol == inventoryToSell)?.Units > 0)
        {
            var units = Math.Min(tradeGood.TradeVolume, ship.Cargo.Inventory.Single(i => i.Symbol == inventoryToSell).Units);
            sellCargoResponse = await _marketplacesService.SellAsync(ship.Symbol, tradeGood.Symbol, units);
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
        if (ship.Cargo.Units == 0)
        {
            return null;
        }

        var sellModels = await _tradesService.GetSellModelsAsyncWithBurn2([ship.Nav.SystemSymbol], ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
        sellModels = sellModels.Where(tm => tm.TradeSymbol == inventoryToSell).ToList();
     
        Cargo? cargo = null;
        if (!sellModels.Any())
        {
            foreach (var inventory in ship.Cargo.Inventory)
            {
                cargo = await _shipsService.JettisonAsync(ship.Symbol, inventory.Symbol, inventory.Units);
            }
        }
        
        return cargo;
    }

    public async Task<(Nav?, Fuel?, Cooldown)> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Units == 0
            || ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
        {
            return (null, null, ship.Cooldown);
        }

        var inventoryToSell = ship.Cargo.Inventory.OrderByDescending(i => i.Units).ThenBy(i => i.Symbol).First().Symbol;
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
        var sellModels = await _tradesService.GetSellModelsAsyncWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var inventorySellModels = sellModels.Where(sm => sm.TradeSymbol == inventoryToSell).ToList();
        var bestSellModel = inventorySellModels.OrderByDescending(sm => sm.NavigationFactor).First();
        return await NavigateHelper(ship, bestSellModel.WaypointSymbol);
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

        var pathDictionary = PathsService.BuildSystemPathWithCost(system.Waypoints.ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var path = pathDictionary.Single(p => p.WaypointSymbol == jumpGateWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(path.PathWaypointSymbols[1], ship);

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
        //var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        //var pathSymbols = paths.Select(p => p.Key.Symbol);
        var inventoryWaypoints = system.Waypoints.Where(w =>
            w.Marketplace is not null
            && w.Marketplace.Exports.Any(e => constructionInventoryNeededSymbols.Contains(e.Symbol))
            && w.Marketplace.TradeGoods is not null);
        var highestSupplyWaypoint = inventoryWaypoints
            .Select(w => new
            {
                Waypoint = w,
                HighestSupply = w.Marketplace?.TradeGoods?
                    .Where(tg => constructionInventoryNeededSymbols.Contains(tg.Symbol))
                    .Max(tg => Enum.Parse<SupplyEnum>(tg.Supply))
            })
            .OrderByDescending(x => x.HighestSupply)
            .ThenBy(w => w.Waypoint.Symbol)
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
        var paths = PathsService.BuildSystemPathWithCost(system.Waypoints.ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var pathSymbols = paths.Select(p => p.WaypointSymbol);
        var inventoryWaypoints = system.Waypoints.Where(w =>
            w.Marketplace is not null
            && w.Marketplace.Exports.Any(e => constructionInventoryNeededSymbols.Contains(e.Symbol))
            && w.Marketplace.TradeGoods is not null);
        var highestSupplyWaypoint = inventoryWaypoints
            .Select(w => new
            {
                Waypoint = w,
                HighestSupply = w.Marketplace?.TradeGoods?
                    .Where(tg => constructionInventoryNeededSymbols.Contains(tg.Symbol))
                    .Max(tg => Enum.Parse<SupplyEnum>(tg.Supply))
            })
            .OrderByDescending(x => x.HighestSupply)
            .ThenBy(w => w.Waypoint.Symbol)
            .ThenBy(x => paths.Single(p => p.WaypointSymbol == x.Waypoint.Symbol).PathWaypointSymbols.Count())
            .FirstOrDefault();

        return highestSupplyWaypoint?.Waypoint.Marketplace?.TradeGoods?.FirstOrDefault(tg =>
            constructionInventoryNeededSymbols.Contains(tg.Symbol)
            && tg.Supply == highestSupplyWaypoint.HighestSupply.ToString());
    }

    public async Task<(Nav?, Fuel?)> NavigateToMarketplaceExport(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint)
    {
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var marketplaceWaypoint = GetConstructionInventoryWaypoint(
                ship,
                currentWaypoint,
                constructionWaypoint,
                system);

        var pathDictionary = PathsService.BuildSystemPathWithCost(system.Waypoints.ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var shortestPath = pathDictionary.SingleOrDefault(p => p.WaypointSymbol == marketplaceWaypoint.Symbol);
        var (nav, fuel) = await _shipsService.NavigateAsync(shortestPath.PathWaypointSymbols[1], ship);
        return (nav, fuel);
    }

    public async Task<(Nav?, Fuel?)> NavigateToSurvey(Ship ship, Waypoint currentWaypoint)
    {
        if (currentWaypoint.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString())
        {
            return (null, null);
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var paths = PathsService.BuildSystemPathWithCost(system.Waypoints.ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var asteroidWaypoints = system.Waypoints.Where(w =>
            w.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()).ToList();
        var asteroidPaths = paths.Where(p => asteroidWaypoints.Select(w => w.Symbol).Contains(p.WaypointSymbol)).ToList();
        var closestAsteroidPath = asteroidPaths.OrderBy(p => p.PathWaypointSymbols.Count).FirstOrDefault();
        var (nav, fuel, cooldown) = await NavigateHelper(ship, closestAsteroidPath.PathWaypointSymbols[1]);
        return (nav, fuel);
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

    public async Task<(Nav? nav, Fuel? fuel, Cooldown cooldown)> NavigateToShipyard(Ship ship, Waypoint currentWaypoint)
    {
        var shipStatuses = await _shipStatusesCacheService.GetAsync();
        var ships = shipStatuses.Select(ss => ss.Ship);
        // var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);

        var (shipyardWaypointSymbol, shipToBuy) = await ShipToBuy(ships);
        if (shipToBuy is null || shipyardWaypointSymbol is null) return (null, null, ship.Cooldown);

        return await NavigateHelper(ship, shipyardWaypointSymbol);

        // var systems = await _systemsService.GetAsync();
        // var traversableSystems = SystemsService.Traverse(systems, ship.Nav.WaypointSymbol);
        // var paths = await _pathsService.BuildSystemPathWithCostWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
        // var shortestPath = paths.SingleOrDefault(p => p.WaypointSymbol == shipyardWaypointSymbol);

        // if (WaypointsService.ExtractSystemFromWaypoint(shortestPath.PathWaypoints[1].WaypointSymbol) != ship.Nav.SystemSymbol)
        // {
        //     var (navJump, cooldownJump) = await _shipsService.JumpAsync(shortestPath.PathWaypointSymbols[1], ship.Symbol);
        //     return (navJump, ship.Fuel, cooldownJump);
        // }
        // if (shortestPath.PathWaypointSymbols.Count() == 1)
        // {

        //     await _waypointsService.GetAsync(shortestPath.WaypointSymbol, refresh: true);
        //     return (null, null, ship.Cooldown);
        // }
        // var (nav, fuel) = await _shipsService.NavigateAsync(shortestPath.PathWaypointSymbols[1], ship);
        // return (nav, fuel, ship.Cooldown);
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
        var (shipyardWaypoint, shipToBuy) = await ShipToBuy(ships);
        if (shipToBuy is null
            || shipyardWaypoint is null
            || ship.Nav.WaypointSymbol != shipyardWaypoint)
        {
            return null;
        }
        
        var response = await _shipyardsService.PurchaseShipAsync(currentWaypoint.Symbol, shipToBuy.Value.ToString());
        return response;
    }

    public async Task<(string?, ShipTypesEnum?)> ShipToBuy(IEnumerable<Ship> ships)
    {
        const long INITIAL_SURVEYOR_SHIP_CREDITS_THRESHOLD = 50_000;
        const long PURCHASE_SHIP_CREDITS_THRESHOLD = 800_000;

        var agent = await _agentsService.GetAsync();
        var systems = await _systemsService.GetAsync();
        var headquartersSystemSymbol = WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters);
        var headquartersSystem = systems.Single(s => s.Symbol == headquartersSystemSymbol);
        var reachableSystems = SystemsService.Traverse(systems, headquartersSystemSymbol);

        if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.SURVEYOR.ToString()) < 1
            && headquartersSystem.Waypoints.Any(w => w.JumpGate is not null && w.IsUnderConstruction)
            && agent.Credits > INITIAL_SURVEYOR_SHIP_CREDITS_THRESHOLD)
        {
            var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_SURVEYOR.ToString()) == true);
            return (shipyard.Symbol, ShipTypesEnum.SHIP_SURVEYOR);
        }

        if (agent.Credits < PURCHASE_SHIP_CREDITS_THRESHOLD)
        {
            return (null, null);
        }

        if (headquartersSystem.Waypoints.Any(w => w.JumpGate is not null && w.IsUnderConstruction))
        {
            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()) < 1)
            {
                var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_LIGHT_HAULER.ToString()) == true);
                return (shipyard.Symbol, ShipTypesEnum.SHIP_LIGHT_HAULER);
            }

            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.EXCAVATOR.ToString()
                && s.Mounts.Any(m => m.Symbol.Contains("MINING_LASER"))) < 8)
            {
                var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_MINING_DRONE.ToString()) == true);
                return (shipyard.Symbol, ShipTypesEnum.SHIP_MINING_DRONE);
            }

            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.EXCAVATOR.ToString()
                && s.Mounts.Any(m => m.Symbol.Contains("GAS_SIPHON"))) < 8)
            {
                var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_SIPHON_DRONE.ToString()) == true);
                return (shipyard.Symbol, ShipTypesEnum.SHIP_SIPHON_DRONE);
            }
        }

        if (headquartersSystem.Waypoints.Any(w => w.JumpGate is not null && !w.IsUnderConstruction))
        {
            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.TRANSPORT.ToString()) < 5)
            {
                var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_LIGHT_SHUTTLE.ToString()) == true);
                return (shipyard.Symbol, ShipTypesEnum.SHIP_LIGHT_SHUTTLE);
            }

            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()) < 10)
            {
                var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_LIGHT_HAULER.ToString()) == true);
                return (shipyard.Symbol, ShipTypesEnum.SHIP_LIGHT_HAULER);
            }
        }

        if (headquartersSystem.Waypoints.Any(w => w.JumpGate is not null && w.IsUnderConstruction))
        {
            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()) < 4)
            {
                var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_LIGHT_HAULER.ToString()) == true);
                return (shipyard.Symbol, ShipTypesEnum.SHIP_LIGHT_HAULER);
            }
        }
        
        if (headquartersSystem.Waypoints.Any(w => w.JumpGate is not null && !w.IsUnderConstruction))
        {
            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.TRANSPORT.ToString()) < 15)
            {
                var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_LIGHT_SHUTTLE.ToString()) == true);
                return (shipyard.Symbol, ShipTypesEnum.SHIP_LIGHT_SHUTTLE);
            }

            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()) < 20)
            {
                var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_LIGHT_HAULER.ToString()) == true);
                return (shipyard.Symbol, ShipTypesEnum.SHIP_LIGHT_HAULER);
            }
        }

        if (headquartersSystem.Waypoints.Any(w => w.JumpGate is not null && !w.IsUnderConstruction))
        {
            // if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.TRANSPORT.ToString()) < 30)
            // {
            //     var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_LIGHT_SHUTTLE.ToString()) == true);
            //     return (shipyard.Symbol, ShipTypesEnum.SHIP_LIGHT_SHUTTLE);
            // }

            if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()) < 50)
            {
                var shipyard = headquartersSystem.Waypoints.Single(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_LIGHT_HAULER.ToString()) == true);
                return (shipyard.Symbol, ShipTypesEnum.SHIP_LIGHT_HAULER);
            }
        }

        foreach (var system in reachableSystems)
        {
            var markets = system.Waypoints.Where(w => w.Marketplace is not null).ToList();
            var probes = ships.Where(s => s.Registration.Role == ShipRegistrationRolesEnum.SATELLITE.ToString() && s.Nav.SystemSymbol == system.Symbol).ToList();
            if (probes.Count < markets.Count) 
            {
                var shipyard = system.Waypoints.OrderBy(w => w.Symbol).FirstOrDefault(w => w.Shipyard?.ShipTypes.Any(st => st.Type == ShipTypesEnum.SHIP_PROBE.ToString()) == true);
                if (shipyard is not null) return (shipyard.Symbol, ShipTypesEnum.SHIP_PROBE);
            }
        }

        return (null, null);
    }

    public async Task<bool> CheckRemotePurchaseShip(IEnumerable<Ship> ships, string shipyardWaypointSymbol, ShipTypesEnum shipType)
    {
        if (ships.Any(s => s.Nav.WaypointSymbol == shipyardWaypointSymbol && s.Nav.Status == NavStatusEnum.DOCKED.ToString()))
        {
            var shipyardWaypoint = await _waypointsService.GetAsync(shipyardWaypointSymbol);
            var agent = await _agentsService.GetAsync();
            if (shipyardWaypoint.Shipyard.ShipFrames.Single(st => st.Type == shipType.ToString()).PurchasePrice + 200_000 < (agent.Credits))
            {
                var purchaseShipResponse = await _shipyardsService.PurchaseShipAsync(shipyardWaypointSymbol, shipType.ToString());
                await _shipStatusesCacheService.SetAsync(new ShipStatus(purchaseShipResponse.Ship, $"Newly purchase ship.", DateTime.UtcNow));
                await _agentsService.SetAsync(purchaseShipResponse.Agent);            
                return true;
            }
        }
        return false;
    }


    private readonly SemaphoreSlim _semaphore = new(1, 1);
    public async Task<Cargo?> TransferCargo(Ship ship, Waypoint currentWaypoint)
    {
        if (currentWaypoint.Type != WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()
            || ship.Cargo.Units != ship.Cargo.Capacity)
        {
            return null;
        }

        await _semaphore.WaitAsync();
        try
        {
            var shipStatuses = await _shipStatusesCacheService.GetAsync();
            var ships = shipStatuses.Select(s => s.Ship).ToList();
            var hauler = ships
                .Where(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString() 
                    && s.Cargo.Units < s.Cargo.Capacity
                    && s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.HaulingAssistToSellAnywhere
                    && s.Nav.WaypointSymbol == ship.Nav.WaypointSymbol
                    && s.Nav.Status == NavStatusEnum.IN_ORBIT.ToString())
                .OrderByDescending(s => s.Cargo.Units)
                .FirstOrDefault();
            if (hauler is null)
            {
                return null;
            }

            while (hauler.Cargo.Units < hauler.Cargo.Capacity
                && ship.Cargo.Units > 0)
            {
                var inventory = ship.Cargo.Inventory.OrderByDescending(i => i.Units).First();
                var inventoryAmount = Math.Min(inventory.Units, hauler.Cargo.Capacity - hauler.Cargo.Units);
                try
                {
                    var transferCargoResult = await _shipsService.TransferCargo(ship.Symbol, hauler.Symbol, inventory.Symbol, inventoryAmount);
                    ship = ship with { Cargo = transferCargoResult.Cargo };
                    hauler = hauler with { Cargo = transferCargoResult.TargetCargo };
                }
                catch
                {
                    ship = await _shipsService.GetAsync(ship.Symbol);        
                    var miningShipStatus = await _shipStatusesCacheService.GetAsync(hauler.Symbol);
                    miningShipStatus = miningShipStatus with { Ship = hauler };
                    await _shipStatusesCacheService.SetAsync(miningShipStatus);

                    hauler = await _shipsService.GetAsync(hauler.Symbol);
                    var haulerShipStatus = await _shipStatusesCacheService.GetAsync(hauler.Symbol);
                    haulerShipStatus = haulerShipStatus with { Ship = hauler };
                    await _shipStatusesCacheService.SetAsync(haulerShipStatus);
                    throw;
                }
            }

            var shipStatus = await _shipStatusesCacheService.GetAsync(hauler.Symbol);
            shipStatus = shipStatus with { Ship = hauler };
            await _shipStatusesCacheService.SetAsync(shipStatus);
        }
        finally
        {
            _semaphore.Release();
        }

        return ship.Cargo;
    }
    public async Task<GoalModel?> GetShipModuleGoalModel(Ship ship)
    {
        if (ship.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()
            && ship.Modules.Any(m => m.Symbol == ShipModuleEnum.MODULE_CARGO_HOLD_II.ToString()))
        {
            var moduleToUpgrade = ShipModuleEnum.MODULE_CARGO_HOLD_II.ToString();
            var systems = await _systemsService.GetAsync();
            var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol, int.MaxValue);
            var waypoints = traversableSystems.SelectMany(s => s.Waypoints);
            var upgradeModuleWaypoints = waypoints.Where(w => 
                w.Marketplace?.Exchange.Any(e => e.Symbol == TradeSymbolsEnum.MODULE_CARGO_HOLD_III.ToString()) == true
                || waypoints.Any(w => w.Marketplace?.Imports.Any(e => e.Symbol == TradeSymbolsEnum.MODULE_CARGO_HOLD_III.ToString()) == true)
                || waypoints.Any(w => w.Marketplace?.Exports.Any(e => e.Symbol == TradeSymbolsEnum.MODULE_CARGO_HOLD_III.ToString()) == true));
            upgradeModuleWaypoints = upgradeModuleWaypoints.OrderBy(w => w.Symbol); // TODO: Get closest
            if (upgradeModuleWaypoints.Any())
            {
                return new GoalModel(moduleToUpgrade, upgradeModuleWaypoints.First().Symbol, null);
            }
        }
        return null;
    }
}