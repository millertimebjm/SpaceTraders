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

    public async Task<PurchaseCargoResult?> PurchaseCargo(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count > 0
            || ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString() 
            || currentWaypoint.Marketplace is null
            || (!currentWaypoint.Marketplace.Exports.Any()))
        {
            return null;
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var waypointsWithinRange = paths.Select(p => p.Key);
        var waypointSymbolsWithinRange = waypointsWithinRange.Select(wwr => wwr.Symbol).ToList();        

        // Find closest marketplace with exports, with imports reachable, with supply at least MODERATE
        var moderateTradeGoodsWithinRange = system
            .Waypoints
            .Where(w => waypointSymbolsWithinRange.Contains(w.Symbol)
                && w.Marketplace is not null
                && w.Marketplace.TradeGoods is not null
                && w.Marketplace.TradeGoods.Any(tg =>
                    tg.Type == "EXPORT"
                    && ((SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.LIMITED)
                    && system.Waypoints.Any(w2 => waypointSymbolsWithinRange.Contains(w2.Symbol)
                        && w2.Marketplace is not null
                        && w2.Marketplace.Imports.Any(i => tg.Symbol == i.Symbol))));

        PurchaseCargoResult? purchaseCargoResult = null;
        if (moderateTradeGoodsWithinRange.Any(w => w.Symbol == currentWaypoint.Symbol))
        {
            //var pathsToModerateTradeGoods = paths.Where(p => moderateTradeGoodsWithinRange.Select(w => w.Symbol).Contains(p.Key.Symbol)).ToList();
            //var shortestPath = pathsToModerateTradeGoods.OrderBy(ptmtg => ptmtg.Value.Item1.Count).ThenBy(ptmtg => ptmtg.Value.Item4).First();
            //return await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship.Symbol);

            var marketplace = currentWaypoint.Marketplace;
            var inventoriesToBuy = marketplace.TradeGoods.Where(tg =>
                tg.Type == "EXPORT"
                && ((SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.LIMITED)
                && system.Waypoints.Any(w2 => waypointSymbolsWithinRange.Contains(w2.Symbol)
                    && w2.Marketplace is not null
                        && w2.Marketplace.Imports.Any(i => tg.Symbol == i.Symbol)));
            var inventoryToBuy = inventoriesToBuy.OrderBy(i => Enum.Parse<SupplyEnum>(i.Supply)).Last();

            purchaseCargoResult = await _marketplacesService.PurchaseAsync(ship.Symbol, inventoryToBuy.Symbol, Math.Min(inventoryToBuy.TradeVolume, ship.Cargo.Capacity - ship.Cargo.Units));
            ship = ship with { Cargo = purchaseCargoResult.Cargo };
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        }

        return purchaseCargoResult;
    }

    public async Task<PurchaseCargoResult?> BuyForConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint)
    {
        if (ship.Cargo.Capacity == ship.Cargo.Units) return null;

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
            // var exports = currentWaypoint.Marketplace.Exports.Select(e => e.Symbol).ToList();

            // var inventoryToBuy = currentWaypoint
            // .Marketplace
            // .TradeGoods
            // .Where(tg => exports.Contains(tg.Symbol) && (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.MODERATE
            //     && system.Waypoints.Where(w => paths.Select(p => p.Key.Symbol).Contains(w.Symbol)).Any(w => w.Marketplace is not null && w.Marketplace.Imports.Select(i => i.Symbol).Contains(tg.Symbol)))
            // .OrderByDescending(tg => (SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply))
            // .FirstOrDefault();
            // if (inventoryToBuy is not null)
            // {
            //     shouldDock = true;
            // }
            var waypointsWithinRange = paths.Select(p => p.Key);
            var waypointSymbolsWithinRange = waypointsWithinRange.Select(wwr => wwr.Symbol).ToList();

            var moderateTradeGoodsWithinRange = system
                .Waypoints
                .Where(w => waypointSymbolsWithinRange.Contains(w.Symbol)
                    && w.Marketplace is not null
                    && w.Marketplace.TradeGoods is not null
                    && w.Marketplace.TradeGoods.Any(tg =>
                        tg.Type == "EXPORT"
                        && ((SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.LIMITED)
                        && system.Waypoints.Any(w2 => waypointSymbolsWithinRange.Contains(w2.Symbol)
                            && w2.Marketplace is not null
                            && w2.Marketplace.Imports.Any(i => tg.Symbol == i.Symbol))));
            if (moderateTradeGoodsWithinRange.Any())
            {
                var pathsToModerateTradeGoods = paths.Where(p => moderateTradeGoodsWithinRange.Select(w => w.Symbol).Contains(p.Key.Symbol)).ToList();
                var shortestPath = pathsToModerateTradeGoods.OrderBy(ptmtg => ptmtg.Value.Item1.Count).ThenBy(ptmtg => ptmtg.Value.Item4).First();
                //return await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship.Symbol);
                if (shortestPath.Key.Symbol == currentWaypoint.Symbol)
                {
                    shouldDock = true;
                }
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
        if (ship.Cargo.Units == ship.Cargo.Capacity
            || !(currentWaypoint.Type == WaypointTypesEnum.ASTEROID.ToString()
                || currentWaypoint.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString()))
        {
            return (null, null);
        }

        ExtractionResult? extractionResult = null;
        while (extractionResult is null)
        {
            IEnumerable<Survey> surveys = (await _surveysCacheService
                    .GetAsync(currentWaypoint.Symbol))
                    .OrderBy(s => s.Expiration);
            try
            {
                var survey = surveys.FirstOrDefault();

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
                    await _surveysCacheService.DeleteAsync(surveys.First().Signature);
                }
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
            await Task.Delay(2000);
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

    public async Task<(Nav?, Fuel?)> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint)
    {
        if (ship.Cargo.Inventory.Count == 0)
        {
            return (null, null);
        }

        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var lowSupplyWaypoint = FindImportDestinationWithLowestSupply(ship, currentWaypoint, system);
        if (lowSupplyWaypoint is not null)
        {
            var pathItem = pathDictionary.SingleOrDefault(p => p.Key.Symbol == lowSupplyWaypoint);
            return await _shipsService.NavigateAsync(pathItem.Value.Item1[1].Symbol, ship.Symbol);
        }

        var unmappedWaypointInventory = system.Waypoints.Where(w =>
            w.Marketplace is not null
            && (w.Marketplace.Exchange.Any(e => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(e.Symbol))
                || w.Marketplace.Imports.Any(e => ship.Cargo.Inventory.Select(i => i.Symbol).Contains(e.Symbol))));
        var paths = pathDictionary.Where(p => unmappedWaypointInventory.Select(e => e.Symbol).Contains(p.Key.Symbol));

        var orderedExchanges = paths.OrderBy(p => p.Value.Item1.Count());
        if (!orderedExchanges.Any())
        {
            throw new NotImplementedException();
        }
        var closestExchange = orderedExchanges.First();
        return await _shipsService.NavigateAsync(closestExchange.Value.Item1[1].Symbol, ship.Symbol);
    }

    public static string? FindImportDestinationWithLowestSupply(Ship ship, Waypoint currentWaypoint, STSystem system)
    {
        if (ship.Cargo.Inventory.Count == 0)
        {
            return null;
        }

        var cargoSymbols = ship.Cargo.Inventory.Select(i => i.Symbol).ToHashSet();
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

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

        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var path = pathDictionary.Single(p => p.Key.Symbol == jumpGateWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(path.Value.Item1[1].Symbol, ship.Symbol);

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
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
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
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
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
        var pathDictionary = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

        var marketplaceWaypoint = await GetConstructionInventoryWaypoint(
                ship,
                currentWaypoint,
                constructionWaypoint,
                system);
        var shortestPath = pathDictionary.Single(p => p.Key.Symbol == marketplaceWaypoint.Symbol);

        var (nav, fuel) = await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship.Symbol);
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

    public async Task<(Nav? nav, Fuel? fuel, bool noWork)> NavigateToMarketplaceRandomExport(Ship ship, Waypoint currentWaypoint)
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
            var navigateResponse = await _shipsService.NavigateAsync(shortestPath.Key.Symbol, ship.Symbol);
            return (navigateResponse.Item1, navigateResponse.Item2, false);
        }

        // Find closest marketplace with exports, with imports reachable, with supply at least MODERATE
        var moderateTradeGoodsWithinRange = system
            .Waypoints
            .Where(w => waypointSymbolsWithinRange.Contains(w.Symbol)
                && w.Marketplace is not null
                && w.Marketplace.TradeGoods is not null
                && w.Marketplace.TradeGoods.Any(tg =>
                    tg.Type == "EXPORT"
                    && ((SupplyEnum)Enum.Parse(typeof(SupplyEnum), tg.Supply) >= SupplyEnum.LIMITED)
                    && system.Waypoints.Any(w2 => waypointSymbolsWithinRange.Contains(w2.Symbol)
                        && w2.Marketplace is not null
                        && w2.Marketplace.Imports.Any(i => tg.Symbol == i.Symbol))));
        if (moderateTradeGoodsWithinRange.Any())
        {
            var pathsToModerateTradeGoods = paths.Where(p => moderateTradeGoodsWithinRange.Select(w => w.Symbol).Contains(p.Key.Symbol)).ToList();
            var shortestPath = pathsToModerateTradeGoods.OrderBy(ptmtg => ptmtg.Value.Item1.Count).ThenBy(ptmtg => ptmtg.Value.Item4).First();
            var navigateResponse = await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship.Symbol);
            return (navigateResponse.Item1, navigateResponse.Item2, false);
        }
        else
        {
            return (null, null, true);
            // throw new NotImplementedException("Trade Goods not mapped or no Limited or higher trade goods.");
            // var anyExportswithImportsWithinRange = system
            // .Waypoints
            // .Where(w => waypointSymbolsWithinRange.Contains(w.Symbol)
            //     && w.Marketplace is not null
            //     && w.Marketplace.Exports is not null
            //     && w.Marketplace.Exports.Any(tg =>
            //         system.Waypoints.Any(w2 => waypointSymbolsWithinRange.Contains(w2.Symbol)
            //             && w2.Marketplace is not null
            //             && w2.Marketplace.Imports.Any(i => tg.Symbol == i.Symbol))));
            // var pathsToAnyExports = paths.Where(p => anyExportswithImportsWithinRange.Select(w => w.Symbol).Contains(p.Key.Symbol)).ToList();
            // var shortestPath = pathsToAnyExports.OrderBy(ptmtg => ptmtg.Value.Item1.Count).ThenBy(ptmtg => ptmtg.Value.Item4).First();
            // return await _shipsService.NavigateAsync(shortestPath.Value.Item1[1].Symbol, ship.Symbol);
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
        var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        var shipToBuy = await ShipToBuy(ships, system);
        if (shipToBuy is null) return (null, null);

        var shipyards = system.Waypoints.Where(w => w.Shipyard is not null);
        var unmappedShipyards = shipyards.Where(s => s.Shipyard.ShipFrames is null);
        if (unmappedShipyards.Any())
        {
            var shipyardPaths = paths.Where(p => unmappedShipyards.Select(s => s.Symbol).Contains(p.Key.Symbol));
            var closestShipyard = shipyardPaths.OrderBy(s => s.Value.Item1.Count()).ThenByDescending(s => s.Value.Item4).First();
            return await _shipsService.NavigateAsync(closestShipyard.Value.Item1[1].Symbol, ship.Symbol);
        }

        var shipyardWithShip = shipyards.FirstOrDefault(s => s.Shipyard.ShipFrames.Any(sf => sf.Type == shipToBuy.ToString()));
        if (shipyardWithShip is not null && ship.Nav.WaypointSymbol != shipyardWithShip.Symbol)
        {
            var path = paths.SingleOrDefault(p => p.Key.Symbol == shipyardWithShip.Symbol);
            var (nav, fuel) = await _shipsService.NavigateAsync(path.Value.Item1[1].Symbol, ship.Symbol);
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
        var ships = await _shipsService.GetAsync();
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