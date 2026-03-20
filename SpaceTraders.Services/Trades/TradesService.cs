using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Trades;

public class TradesService(
    ILogger<TradesService> _logger,
    IPathsService _pathsService,
    IPathsCacheService _pathsCacheService,
    ITradesCacheService _tradesCacheService,
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    IWaypointsService _waypointsService
) : ITradesService
{
    public async Task UpdateTradeModelAsync(string waypointSymbol, IReadOnlyList<TradeGood> tradeGoods)
    {
        var hasTradeGoods = await _tradesCacheService.AnyTradeModelAsync(waypointSymbol);
        if (hasTradeGoods)
        {
            await _tradesCacheService.UpdateTradeModelAsync(waypointSymbol, tradeGoods);
            return;
        }
        await GetTradeModelsWithCacheAsync();
    }

    // public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync(List<Waypoint> waypoints, string originWaypoint, int maxFuel, int currentFuel)
    // {
    //     return await BuildTradeModelWithBurn(waypoints, maxFuel, currentFuel, originWaypoint);
    // }

    // public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync()
    // {
    //     var tradeModels = await _tradesCacheService.GetTradeModelsAsync();
    //     if (tradeModels.Count == 0)
    //     {
    //         await BuildTradeModel();
    //     }
    //     tradeModels = await _tradesCacheService.GetTradeModelsAsync();
    //     return tradeModels;
    // }

    // public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync(string originWaypoint, int maxFuel, int startingFuel)
    // {
    //     var tradeModels = await _tradesCacheService.GetTradeModelsAsync();
    //     if (tradeModels.Count == 0)
    //     {
    //         await BuildTradeModel();
    //     }
    //     tradeModels = await _tradesCacheService.GetTradeModelsAsync();
    //     List<TradeModel> tradeModelsFromOrigin = new();
    //     var paths = await _pathsService.BuildSystemPathWithCost(originWaypoint, maxFuel, startingFuel);
    //     foreach (var tradeModel in tradeModels)
    //     {
    //         var path = paths.Single(p => p.WaypointSymbol == tradeModel.ExportWaypointSymbol);
    //         var tradeModelFromOrigin = tradeModel with { TimeCost = tradeModel.TimeCost + path.TimeCost, NavigationFactor = NavigationFactor(tradeModel.TimeCost + path.TimeCost) };
    //         tradeModelsFromOrigin.Add(tradeModelFromOrigin);
    //     }
    //     return tradeModelsFromOrigin;
    // }

    // public async Task BuildTradeModel()
    // {
    //     var agent = await _agentsService.GetAsync();
    //     var systems = await _systemsService.GetAsync();
    //     var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters));
    //     var waypoints = traversableSystems.SelectMany(ts => ts.Waypoints).ToList();
    //     var newTradeModels = await BuildTradeModel(waypoints, 600, 600);
    //     if (newTradeModels.Count == 0) return;
    //     await _tradesCacheService.SaveTradeModelsAsync(newTradeModels);
    // }

    // private async Task<IReadOnlyList<TradeModel>> BuildTradeModel(
    //     IReadOnlyList<Waypoint> waypoints,
    //     int fuelMax,
    //     int fuelCurrent,
    //     string? originWaypoint = null)
    // {
    //     var marketplaceWaypoints = waypoints.Where(w => w.Marketplace is not null && w.Marketplace.TradeGoods is not null).ToList();
    //     ConcurrentBag<TradeModel> tradeModels = new();
    //     var marketplaceWaypointExports = marketplaceWaypoints.Where(w => w.Marketplace.Exports.Any() || w.Marketplace.Exchange.Any()).ToList();
    //     List<PathModel> pathModels = [];
    //     if (originWaypoint is not null)
    //     {
    //         pathModels = PathsService.BuildSystemPathWithCost(waypoints.ToList(), originWaypoint, fuelMax, fuelCurrent);
    //     }

    //     await Parallel.ForEachAsync(marketplaceWaypointExports, async (marketplaceWaypointExport, CancellationToken) =>
    //     //foreach (var marketplaceWaypointExport in marketplaceWaypointExports)
    //     {
    //         var exportTimeCost = pathModels.SingleOrDefault(p => p.WaypointSymbol == marketplaceWaypointExport.Symbol)?.TimeCost ?? 0;
    //         var exports = marketplaceWaypointExport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXPORT.ToString()).ToList();
    //         foreach (var export in exports)
    //         {
    //             var imports = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Imports.Any(i => i.Symbol == export.Symbol)).ToList();
    //             foreach (var marketplaceWaypointImport in imports)
    //             {
    //                 var (navigationFactor, timeCost) = await GetNavigationFactor(waypoints, marketplaceWaypointExport, marketplaceWaypointImport.Symbol, fuelMax, fuelCurrent);
    //                 var marketplaceWaypointImports = marketplaceWaypointImport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.IMPORT.ToString() && tg.Symbol == export.Symbol).ToList();
    //                 foreach (var import in marketplaceWaypointImports)
    //                 {
    //                     tradeModels.Add(new TradeModel(
    //                         export.Symbol,
    //                         marketplaceWaypointExport.Symbol,
    //                         export.PurchasePrice,
    //                         Enum.Parse<SupplyEnum>(export.Supply),
    //                         export.TradeVolume,
    //                         marketplaceWaypointImport.Symbol,
    //                         import.SellPrice,
    //                         Enum.Parse<SupplyEnum>(import.Supply),
    //                         import.TradeVolume,
    //                         navigationFactor,
    //                         timeCost + exportTimeCost
    //                     ));
    //                 }
    //             }
    //             var exchangeMarketplaceWaypoints = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Exchange.Any(i => i.Symbol == export.Symbol)).ToList();
    //             foreach (var marketplaceWaypointExchange in exchangeMarketplaceWaypoints)
    //             {
    //                 var (navigationFactor, timeCost) = await GetNavigationFactor(waypoints, marketplaceWaypointExport, marketplaceWaypointExchange.Symbol, fuelMax, fuelCurrent);
    //                 var tradeGoodImports = marketplaceWaypointExchange.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString() && tg.Symbol == export.Symbol);
    //                 foreach (var import in tradeGoodImports)
    //                 {
    //                     tradeModels.Add(new TradeModel(
    //                         export.Symbol,
    //                         marketplaceWaypointExport.Symbol,
    //                         export.PurchasePrice,
    //                         Enum.Parse<SupplyEnum>(export.Supply),
    //                         export.TradeVolume,
    //                         marketplaceWaypointExchange.Symbol,
    //                         import.SellPrice,
    //                         Enum.Parse<SupplyEnum>(import.Supply),
    //                         import.TradeVolume,
    //                         navigationFactor,
    //                         timeCost + exportTimeCost
    //                     ));
    //                 }
    //             }
    //         }
    //         var exchanges = marketplaceWaypointExport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString()).ToList();
    //         foreach (var exchange in exchanges)
    //         {
    //             var imports = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Imports.Any(i => i.Symbol == exchange.Symbol)).ToList();
    //             foreach (var marketplaceWaypointImport in imports)
    //             {
    //                 var (navigationFactor, timeCost) = await GetNavigationFactor(waypoints, marketplaceWaypointExport, marketplaceWaypointImport.Symbol, fuelMax, fuelCurrent);
    //                 var marketplaceWaypointImports = marketplaceWaypointImport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.IMPORT.ToString() && tg.Symbol == exchange.Symbol).ToList();
    //                 foreach (var import in marketplaceWaypointImports)
    //                 {
    //                     tradeModels.Add(new TradeModel(
    //                         exchange.Symbol,
    //                         marketplaceWaypointExport.Symbol,
    //                         exchange.PurchasePrice,
    //                         Enum.Parse<SupplyEnum>(exchange.Supply),
    //                         exchange.TradeVolume,
    //                         marketplaceWaypointImport.Symbol,
    //                         import.SellPrice,
    //                         Enum.Parse<SupplyEnum>(import.Supply),
    //                         import.TradeVolume,
    //                         navigationFactor,
    //                         timeCost + exportTimeCost
    //                     ));
    //                 }
    //             }
    //             var otherExchangeMarketplaceWaypoints = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Exchange.Any(i => i.Symbol == exchange.Symbol)).ToList();
    //             foreach (var otherMarketplaceWaypointExchange in otherExchangeMarketplaceWaypoints)
    //             {
    //                 var (navigationFactor, timeCost) = await GetNavigationFactor(waypoints, marketplaceWaypointExport, otherMarketplaceWaypointExchange.Symbol, fuelMax, fuelCurrent);
    //                 var tradeGoodImports = otherMarketplaceWaypointExchange.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString() && tg.Symbol == exchange.Symbol);
    //                 foreach (var otherExchange in tradeGoodImports)
    //                 {
    //                     tradeModels.Add(new TradeModel(
    //                         exchange.Symbol,
    //                         marketplaceWaypointExport.Symbol,
    //                         exchange.PurchasePrice,
    //                         Enum.Parse<SupplyEnum>(exchange.Supply),
    //                         exchange.TradeVolume,
    //                         otherMarketplaceWaypointExchange.Symbol,
    //                         otherExchange.SellPrice,
    //                         Enum.Parse<SupplyEnum>(otherExchange.Supply),
    //                         otherExchange.TradeVolume,
    //                         navigationFactor,
    //                         timeCost + exportTimeCost
    //                     ));
    //                 }
    //             }
    //         }
    //     });
    //     //}
    //     return tradeModels.ToList();
    // }

    // private async Task<IReadOnlyList<TradeModel>> BuildTradeModelWithBurn(
    //     IReadOnlyList<Waypoint> waypoints,
    //     int fuelMax,
    //     int fuelCurrent,
    //     string? originWaypoint = null)
    // {
    //     var marketplaceWaypoints = waypoints.Where(w => w.Marketplace is not null && w.Marketplace.TradeGoods is not null).ToList();
    //     ConcurrentBag<TradeModel> tradeModels = new();
    //     var marketplaceWaypointExports = marketplaceWaypoints.Where(w => w.Marketplace.Exports.Any() || w.Marketplace.Exchange.Any(e => e.Symbol != TradeSymbolsEnum.FUEL.ToString() && e.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString())).ToList();
    //     List<PathModelWithBurn> pathModelsWithBurn = [];
    //     if (originWaypoint is not null)
    //     {
    //         pathModelsWithBurn = PathsService.BuildSystemPathWithCostWithBurn(waypoints.ToList(), originWaypoint, fuelMax, fuelCurrent);
    //     }

    //     //await Parallel.ForEachAsync(marketplaceWaypointExports, async (marketplaceWaypointExport, CancellationToken) =>
    //     foreach (var marketplaceWaypointExport in marketplaceWaypointExports)
    //     {
    //         var exportTimeCost = pathModelsWithBurn.SingleOrDefault(p => p.WaypointSymbol == marketplaceWaypointExport.Symbol)?.TimeCost ?? 0;
    //         var exports = marketplaceWaypointExport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXPORT.ToString()).ToList();
    //         foreach (var export in exports)
    //         {
    //             var imports = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Imports.Any(i => i.Symbol == export.Symbol)).ToList();
    //             foreach (var marketplaceWaypointImport in imports)
    //             {
    //                 var (navigationFactor, timeCost) = await GetNavigationFactorWithBurn(waypoints, marketplaceWaypointExport, marketplaceWaypointImport.Symbol, fuelMax, fuelCurrent);
    //                 var marketplaceWaypointImports = marketplaceWaypointImport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.IMPORT.ToString() && tg.Symbol == export.Symbol).ToList();
    //                 foreach (var import in marketplaceWaypointImports)
    //                 {
    //                     tradeModels.Add(new TradeModel(
    //                         export.Symbol,
    //                         marketplaceWaypointExport.Symbol,
    //                         export.PurchasePrice,
    //                         Enum.Parse<SupplyEnum>(export.Supply),
    //                         export.TradeVolume,
    //                         marketplaceWaypointImport.Symbol,
    //                         import.SellPrice,
    //                         Enum.Parse<SupplyEnum>(import.Supply),
    //                         import.TradeVolume,
    //                         navigationFactor,
    //                         timeCost + exportTimeCost
    //                     ));
    //                 }
    //             }
    //             var exchangeMarketplaceWaypoints = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Exchange.Any(i => i.Symbol == export.Symbol)).ToList();
    //             foreach (var marketplaceWaypointExchange in exchangeMarketplaceWaypoints)
    //             {
    //                 var (navigationFactor, timeCost) = await GetNavigationFactorWithBurn(waypoints, marketplaceWaypointExport, marketplaceWaypointExchange.Symbol, fuelMax, fuelCurrent);
    //                 var tradeGoodImports = marketplaceWaypointExchange.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString() && tg.Symbol == export.Symbol);
    //                 foreach (var import in tradeGoodImports)
    //                 {
    //                     tradeModels.Add(new TradeModel(
    //                         export.Symbol,
    //                         marketplaceWaypointExport.Symbol,
    //                         export.PurchasePrice,
    //                         Enum.Parse<SupplyEnum>(export.Supply),
    //                         export.TradeVolume,
    //                         marketplaceWaypointExchange.Symbol,
    //                         import.SellPrice,
    //                         Enum.Parse<SupplyEnum>(import.Supply),
    //                         import.TradeVolume,
    //                         navigationFactor,
    //                         timeCost + exportTimeCost
    //                     ));
    //                 }
    //             }
    //         }
    //         var exchanges = marketplaceWaypointExport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString() && tg.Symbol != TradeSymbolsEnum.FUEL.ToString() && tg.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString()).ToList();
    //         foreach (var exchange in exchanges)
    //         {
    //             var imports = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Imports.Any(i => i.Symbol == exchange.Symbol)).ToList();
    //             foreach (var marketplaceWaypointImport in imports)
    //             {
    //                 var (navigationFactor, timeCost) = await GetNavigationFactorWithBurn(waypoints, marketplaceWaypointExport, marketplaceWaypointImport.Symbol, fuelMax, fuelCurrent);
    //                 var marketplaceWaypointImports = marketplaceWaypointImport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.IMPORT.ToString() && tg.Symbol == exchange.Symbol).ToList();
    //                 foreach (var import in marketplaceWaypointImports)
    //                 {
    //                     tradeModels.Add(new TradeModel(
    //                         exchange.Symbol,
    //                         marketplaceWaypointExport.Symbol,
    //                         exchange.PurchasePrice,
    //                         Enum.Parse<SupplyEnum>(exchange.Supply),
    //                         exchange.TradeVolume,
    //                         marketplaceWaypointImport.Symbol,
    //                         import.SellPrice,
    //                         Enum.Parse<SupplyEnum>(import.Supply),
    //                         import.TradeVolume,
    //                         navigationFactor,
    //                         timeCost + exportTimeCost
    //                     ));
    //                 }
    //             }
    //             var otherExchangeMarketplaceWaypoints = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Exchange.Any(i => i.Symbol == exchange.Symbol)).ToList();
    //             foreach (var otherMarketplaceWaypointExchange in otherExchangeMarketplaceWaypoints)
    //             {
    //                 var (navigationFactor, timeCost) = await GetNavigationFactorWithBurn(waypoints, marketplaceWaypointExport, otherMarketplaceWaypointExchange.Symbol, fuelMax, fuelCurrent);
    //                 var tradeGoodImports = otherMarketplaceWaypointExchange.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString() && tg.Symbol == exchange.Symbol);
    //                 foreach (var otherExchange in tradeGoodImports)
    //                 {
    //                     tradeModels.Add(new TradeModel(
    //                         exchange.Symbol,
    //                         marketplaceWaypointExport.Symbol,
    //                         exchange.PurchasePrice,
    //                         Enum.Parse<SupplyEnum>(exchange.Supply),
    //                         exchange.TradeVolume,
    //                         otherMarketplaceWaypointExchange.Symbol,
    //                         otherExchange.SellPrice,
    //                         Enum.Parse<SupplyEnum>(otherExchange.Supply),
    //                         otherExchange.TradeVolume,
    //                         navigationFactor,
    //                         timeCost + exportTimeCost
    //                     ));
    //                 }
    //             }
    //         }
    //     //});
    //     }
    //     return tradeModels.ToList();
    // }

    // public async Task<(decimal navigationFactor, int timeCost)> GetNavigationFactor(IReadOnlyList<Waypoint> waypoints, Waypoint exportWaypoint, string importSymbol, int fuelMax, int fuelCurrent)
    // {
    //     var (navigationFactor, timeCost) = await _pathsCacheService.GetNavigationFactor(exportWaypoint.Symbol, importSymbol, fuelMax, fuelCurrent);
    //     if (navigationFactor is not null) return (navigationFactor.Value, timeCost ?? 0); 

    //     try
    //     {
    //         var paths = PathsService.BuildSystemPathWithCost(waypoints.ToList(), exportWaypoint.Symbol, fuelMax, fuelCurrent);
    //         var path = paths.Single(p => p.WaypointSymbol == importSymbol);
    //         navigationFactor = NavigationFactor(path.TimeCost);
    //         await _pathsCacheService.SetNavigationFactor(exportWaypoint.Symbol, importSymbol, fuelMax, fuelCurrent, navigationFactor.Value, path.TimeCost);
    //         return (navigationFactor.Value, path.TimeCost);
    //     }
    //     catch(Exception ex)
    //     {
    //         _logger.LogError("Paths were not updated because a new system became available.");
    //         await _pathsCacheService.ClearAllCachedSystemPaths();

    //         var paths = PathsService.BuildSystemPathWithCost(waypoints.ToList(), exportWaypoint.Symbol, fuelMax, fuelCurrent);
    //         var path = paths.Single(p => p.WaypointSymbol == importSymbol);
    //         navigationFactor = NavigationFactor(path.TimeCost);
    //         await _pathsCacheService.SetNavigationFactor(exportWaypoint.Symbol, importSymbol, fuelMax, fuelCurrent, navigationFactor.Value, path.TimeCost);
    //         return (navigationFactor.Value, path.TimeCost);
    //     }
    // }

    // public async Task<(decimal navigationFactor, int timeCost)> GetNavigationFactorWithBurn(IReadOnlyList<Waypoint> waypoints, Waypoint exportWaypoint, string importSymbol, int fuelMax, int fuelCurrent)
    // {
    //     var (navigationFactor, timeCost) = await _pathsCacheService.GetNavigationFactor(exportWaypoint.Symbol, importSymbol, fuelMax, fuelCurrent);
    //     if (navigationFactor is not null) return (navigationFactor.Value, timeCost ?? 0); 

    //     try
    //     {
    //         var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints.ToList(), exportWaypoint.Symbol, fuelMax, fuelCurrent, importSymbol);
    //         var path = paths.Single(p => p.WaypointSymbol == importSymbol);
    //         navigationFactor = NavigationFactor(path.TimeCost);
    //         await _pathsCacheService.SetNavigationFactor(exportWaypoint.Symbol, importSymbol, fuelMax, fuelCurrent, navigationFactor.Value, path.TimeCost);
    //         return (navigationFactor.Value, path.TimeCost);
    //     }
    //     catch(Exception ex)
    //     {
    //         _logger.LogError("Paths were not updated because a new system became available.");
    //         await _pathsCacheService.ClearAllCachedSystemPaths();

    //         var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints.ToList(), exportWaypoint.Symbol, fuelMax, fuelCurrent, importSymbol);
    //         var path = paths.Single(p => p.WaypointSymbol == importSymbol);
    //         navigationFactor = NavigationFactor(path.TimeCost);
    //         await _pathsCacheService.SetNavigationFactor(exportWaypoint.Symbol, importSymbol, fuelMax, fuelCurrent, navigationFactor.Value, path.TimeCost);
    //         return (navigationFactor.Value, path.TimeCost);
    //     }
    // }

    // // public IReadOnlyList<TradeModel> GetBestOrderedTrades(IReadOnlyList<TradeModel> trades)
    // // {
    // //     const decimal profitWeight = 0.5m;
    // //     const decimal marginWeight = 0.5m;

    // //     var orderedTrades = trades
    // //         .Where(t => t.ImportSellPrice > t.ExportBuyPrice && t.ExportSupplyEnum > SupplyEnum.MODERATE)
    // //         .OrderByDescending(t =>
    // //         {
    // //             var profit = t.ImportSellPrice - t.ExportBuyPrice;
    // //             var marginPercent = (decimal)profit / t.ExportBuyPrice;

    // //             var score =
    // //                 (profitWeight * profit) +
    // //                 (marginWeight * marginPercent * 100); // scale percentage for balance
                
    // //             return score * SupplyFactor(t.ExportSupplyEnum, t.ImportSupplyEnum);
    // //         })
    // //         .ThenBy(t => t.ExportWaypointSymbol)
    // //         .ThenBy(t => t.ImportWaypointSymbol)
    // //         .ToList();
    // //     return orderedTrades;
    // // }

    // public IReadOnlyList<TradeModel> GetBestOrderedTradesWithTravelCost(
    //     IReadOnlyList<TradeModel> trades)
    // {
    //     const decimal profitWeight = 0.5m;
    //     const decimal marginWeight = 0.5m;

    //     var orderedTrades = trades
    //         .Where(t => t.ImportSellPrice > t.ExportBuyPrice)
    //         .OrderByDescending(t =>
    //         {
    //             var profit = t.ImportSellPrice - t.ExportBuyPrice;
    //             var marginPercent = (decimal)profit / t.ExportBuyPrice;

    //             var score =
    //                 (profitWeight * profit) +
    //                 (marginWeight * marginPercent * 100); // scale percentage for balance

    //             score = score * SupplyFactor(t.ExportSupplyEnum, t.ImportSupplyEnum);
    //             score = score * t.NavigationFactor;
    //             return score;
    //         })
    //         .ToList();
    //     return orderedTrades;
    // }

    // public async Task<IReadOnlyList<TradeModel>> GetBestOrderedTradesWithTravelCost(
    //     string originWaypoint,
    //     int fuelMax,
    //     int fuelCurrent)
    // {
    //     var tradeModels = await GetTradeModelsAsync(originWaypoint, fuelMax, fuelCurrent);
    //     return GetBestOrderedTradesWithTravelCost(tradeModels);
    // }

    // public async Task<IReadOnlyList<TradeModel>> GetBestOrderedTradesWithTravelCost()
    // {
    //     var tradeModels = await GetTradeModelsAsync();
    //     return GetBestOrderedTradesWithTravelCost(tradeModels);
    // }

    // public TradeModel? GetBestTrade(IReadOnlyList<TradeModel> trades)
    // {
    //     //var orderedTrades = GetBestOrderedTrades(trades);
    //     var orderedTrades = GetBestOrderedTradesWithTravelCost(trades);
    //     return orderedTrades.FirstOrDefault();
    // }

    // public TradeModel? GetAnyBestTrade(IReadOnlyList<TradeModel> trades)
    // {
    //     var orderedTrades = trades
    //         .OrderByDescending(t =>
    //             (t.ImportSellPrice - t.ExportBuyPrice) *
    //             SupplyFactor(t.ExportSupplyEnum, t.ImportSupplyEnum)
    //         ).ToList();
    //     return orderedTrades.FirstOrDefault();
    // }

    private static decimal SupplyFactor(SupplyEnum export, SupplyEnum import)
    {
        // Assign numeric values to supply levels (tune these based on game logic)
        decimal exportMultiplier = export switch
        {
            SupplyEnum.ABUNDANT => 5,
            SupplyEnum.HIGH => 3,
            SupplyEnum.MODERATE => 1,
            SupplyEnum.LIMITED => 0.5m,
            SupplyEnum.SCARCE => 0.3m,
            _ => 0
        };

        var importMultiplier = SupplyFactorImportMuliplier(import);

        return exportMultiplier * importMultiplier;
    }

    private static decimal SupplyFactorImportMuliplier(SupplyEnum import)
    {
        decimal importMultiplier = import switch
        {
            SupplyEnum.ABUNDANT => 0.3m,
            SupplyEnum.HIGH => 0.5m,
            SupplyEnum.MODERATE => 0.8m,
            SupplyEnum.LIMITED => 1.0m,
            SupplyEnum.SCARCE => 1.5m,
            _ => 0
        };
        return importMultiplier;
    }

    private static decimal NavigationFactor(int cost)
    {
        if (cost <= 200) return 1;
        if (cost <= 400) return .9m;
        if (cost <= 800) return .7m;
        if (cost <= 1200) return .5m;
        if (cost <= 2400) return .25m;
        return .1m;
    }

    // public SellModel? GetBestSellModel(IReadOnlyList<SellModel> sellModels)
    // {
    //     var orderedTrades = sellModels
    //         .OrderByDescending(m => m.SellPrice)
    //         .ThenBy(m => m.WaypointSymbol) // Tiebreaker
    //         .ToList();
    //     return orderedTrades.FirstOrDefault();
    // }

    // public IReadOnlyList<SellModel> BuildSellModel(IReadOnlyList<Waypoint> waypoints, Waypoint? originWaypoint = null, int? fuelMax = null, int? fuelCurrent = null)
    // {
    //     var marketplaceWaypoints = waypoints.Where(w => w.Marketplace is not null && w.Marketplace.TradeGoods is not null).ToList();
    //     List<SellModel> sellModels = new();
    //     List<PathModel> pathModels = [];
    //     if (originWaypoint is not null)
    //     {
    //         pathModels = PathsService.BuildSystemPathWithCost(waypoints.ToList(), originWaypoint.Symbol, fuelMax.Value, fuelCurrent.Value);
    //     }
    //     foreach (var marketplaceWaypoint in marketplaceWaypoints)
    //     {
    //         var marketplacePathTimeCost = pathModels.SingleOrDefault(p => p.WaypointSymbol == marketplaceWaypoint.Symbol)?.TimeCost;
    //         foreach (var tradeGood in marketplaceWaypoint.Marketplace.TradeGoods)
    //         {
    //             sellModels.Add(new SellModel(
    //                 tradeGood.Symbol,
    //                 marketplaceWaypoint.Symbol,
    //                 tradeGood.SellPrice,
    //                 Enum.Parse<SupplyEnum>(tradeGood.Supply),
    //                 tradeGood.TradeVolume,
    //                 NavigationFactor(marketplacePathTimeCost ?? 0),
    //                 marketplacePathTimeCost ?? 0
    //             ));
    //         }
    //     }
    //     return sellModels;
    // }

    public async Task<List<TradeModel>> GetTradeModelsWithCacheAsync()
    {
        var tradeModels = await _tradesCacheService.GetTradeModelsAsync();
        if (tradeModels is null || !tradeModels.Any())
        {
            tradeModels = await BuildTradeModelWithBurnAsync2();
            await _tradesCacheService.SaveTradeModelsAsync(tradeModels);
        }
        return tradeModels.ToList();
    }

    public async Task<List<TradeModel>> GetTradeModelsAsyncWithBurn2(List<string> systemSymbols, string originWaypointSymbol, int fuelMax, int fuelCurrent)
    {
        var tradeModels = await GetTradeModelsWithCacheAsync();

        var localTradeModels = tradeModels.Where(tm => systemSymbols.Any(s => tm.ExportWaypointSymbol.StartsWith(s)) && systemSymbols.Any(s => tm.ImportWaypointSymbol.StartsWith(s))).ToList();
        var systems = await _systemsService.GetAsync();
        var systemsIncluded = systems.Where(s => systemSymbols.Contains(s.Symbol));
        var traversableSystems = SystemsService.Traverse(systemsIncluded, WaypointsService.ExtractSystemFromWaypoint(originWaypointSymbol));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        var pathModels = PathsService.BuildSystemPathWithCostWithBurn(waypoints, originWaypointSymbol, fuelMax, fuelCurrent);
        List<TradeModel> tradeModelsWithOriginTimeCost = [];
        foreach (var localTradeModel in localTradeModels)
        {
            var pathModel = pathModels.Single(pm => pm.WaypointSymbol == localTradeModel.ExportWaypointSymbol);
            var newLocalTradeModel = localTradeModel with { TimeCost = localTradeModel.TimeCost + pathModel.TimeCost };
            newLocalTradeModel = newLocalTradeModel with { NavigationFactor = GetTradeModelNavigationFactorWithBurn2(localTradeModel.ExportBuyPrice, localTradeModel.ImportSellPrice, localTradeModel.ExportSupplyEnum, localTradeModel.ImportSupplyEnum, localTradeModel.TimeCost) };
            tradeModelsWithOriginTimeCost.Add(newLocalTradeModel);
        }

        var tradeModelsWithOriginTimeCostGrouped = tradeModelsWithOriginTimeCost.GroupBy(tm => tm.TradeSymbol);
        var bestTradeModelBySymbol = tradeModelsWithOriginTimeCostGrouped.Select(tmg => tmg.OrderBy(tm => tm.TimeCost).First()).ToList();
        return bestTradeModelBySymbol.OrderBy(tm => tm.TimeCost).ToList();
    }

    private async Task<List<TradeModel>> BuildTradeModelWithBurnAsync2()
    {
        int fuelMax = 600;
        int fuelCurrent = 600;
        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var marketplaceWaypoints = waypoints.Where(w => w.Marketplace is not null && w.Marketplace.TradeGoods is not null).ToList();
        ConcurrentBag<TradeModel> tradeModels = [];
        var marketplaceWaypointExports = marketplaceWaypoints.Where(w => w.Marketplace.Exports.Any() || w.Marketplace.Exchange.Any(e => e.Symbol != TradeSymbolsEnum.FUEL.ToString() && e.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString())).ToList();

        await Parallel.ForEachAsync(marketplaceWaypointExports, async (marketplaceWaypointExport, CancellationToken) =>
        //foreach (var marketplaceWaypointExport in marketplaceWaypointExports)
        {
            var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints, marketplaceWaypointExport.Symbol, 600, 600);
            var exports = marketplaceWaypointExport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXPORT.ToString()).ToList();
            foreach (var export in exports)
            {
                var imports = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Imports.Any(i => i.Symbol == export.Symbol)).ToList();
                foreach (var marketplaceWaypointImport in imports)
                {
                    var timeCost = paths.Single(p => p.WaypointSymbol == marketplaceWaypointImport.Symbol).TimeCost;
                    var marketplaceWaypointImports = marketplaceWaypointImport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.IMPORT.ToString() && tg.Symbol == export.Symbol).ToList();
                    foreach (var import in marketplaceWaypointImports)
                    {
                        var navigationFactor = GetTradeModelNavigationFactorWithBurn2(
                            export.PurchasePrice,
                            import.SellPrice,
                            Enum.Parse<SupplyEnum>(export.Supply),
                            Enum.Parse<SupplyEnum>(import.Supply),
                            timeCost);
                        tradeModels.Add(new TradeModel(
                            export.Symbol,
                            marketplaceWaypointExport.Symbol,
                            export.PurchasePrice,
                            Enum.Parse<SupplyEnum>(export.Supply),
                            export.TradeVolume,
                            marketplaceWaypointImport.Symbol,
                            import.SellPrice,
                            Enum.Parse<SupplyEnum>(import.Supply),
                            import.TradeVolume,
                            navigationFactor,
                            timeCost
                        ));
                    }
                }
                var exchangeMarketplaceWaypoints = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Exchange.Any(i => i.Symbol == export.Symbol)).ToList();
                foreach (var marketplaceWaypointExchange in exchangeMarketplaceWaypoints)
                {
                    var timeCost = paths.Single(p => p.WaypointSymbol == marketplaceWaypointExchange.Symbol).TimeCost;
                    var tradeGoodImports = marketplaceWaypointExchange.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString() && tg.Symbol == export.Symbol);
                    foreach (var import in tradeGoodImports)
                    {
                        var navigationFactor = GetTradeModelNavigationFactorWithBurn2(
                            export.PurchasePrice,
                            import.SellPrice,
                            Enum.Parse<SupplyEnum>(export.Supply),
                            Enum.Parse<SupplyEnum>(import.Supply),
                            timeCost);
                        tradeModels.Add(new TradeModel(
                            export.Symbol,
                            marketplaceWaypointExport.Symbol,
                            export.PurchasePrice,
                            Enum.Parse<SupplyEnum>(export.Supply),
                            export.TradeVolume,
                            marketplaceWaypointExchange.Symbol,
                            import.SellPrice,
                            Enum.Parse<SupplyEnum>(import.Supply),
                            import.TradeVolume,
                            navigationFactor,
                            timeCost
                        ));
                    }
                }
            }
            var exchanges = marketplaceWaypointExport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString() && tg.Symbol != TradeSymbolsEnum.FUEL.ToString() && tg.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString()).ToList();
            foreach (var exchange in exchanges)
            {
                var imports = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Imports.Any(i => i.Symbol == exchange.Symbol)).ToList();
                foreach (var marketplaceWaypointImport in imports)
                {
                    var timeCost = paths.Single(p => p.WaypointSymbol == marketplaceWaypointImport.Symbol).TimeCost;
                    var marketplaceWaypointImports = marketplaceWaypointImport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.IMPORT.ToString() && tg.Symbol == exchange.Symbol).ToList();
                    foreach (var import in marketplaceWaypointImports)
                    {
                        var navigationFactor = GetTradeModelNavigationFactorWithBurn2(
                            exchange.PurchasePrice,
                            import.SellPrice,
                            Enum.Parse<SupplyEnum>(exchange.Supply),
                            Enum.Parse<SupplyEnum>(import.Supply),
                            timeCost);
                        tradeModels.Add(new TradeModel(
                            exchange.Symbol,
                            marketplaceWaypointExport.Symbol,
                            exchange.PurchasePrice,
                            Enum.Parse<SupplyEnum>(exchange.Supply),
                            exchange.TradeVolume,
                            marketplaceWaypointImport.Symbol,
                            import.SellPrice,
                            Enum.Parse<SupplyEnum>(import.Supply),
                            import.TradeVolume,
                            navigationFactor,
                            timeCost
                        ));
                    }
                }
                var otherExchangeMarketplaceWaypoints = marketplaceWaypoints.Where(w => marketplaceWaypointExport.Symbol != w.Symbol && w.Marketplace.Exchange.Any(i => i.Symbol == exchange.Symbol)).ToList();
                foreach (var otherMarketplaceWaypointExchange in otherExchangeMarketplaceWaypoints)
                {
                    var timeCost = paths.Single(p => p.WaypointSymbol == otherMarketplaceWaypointExchange.Symbol).TimeCost;
                    var tradeGoodImports = otherMarketplaceWaypointExchange.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString() && tg.Symbol == exchange.Symbol);
                    foreach (var otherExchange in tradeGoodImports)
                    {
                        var navigationFactor = GetTradeModelNavigationFactorWithBurn2(
                            exchange.PurchasePrice,
                            otherExchange.SellPrice,
                            Enum.Parse<SupplyEnum>(exchange.Supply),
                            Enum.Parse<SupplyEnum>(otherExchange.Supply),
                            timeCost);
                        tradeModels.Add(new TradeModel(
                            exchange.Symbol,
                            marketplaceWaypointExport.Symbol,
                            exchange.PurchasePrice,
                            Enum.Parse<SupplyEnum>(exchange.Supply),
                            exchange.TradeVolume,
                            otherMarketplaceWaypointExchange.Symbol,
                            otherExchange.SellPrice,
                            Enum.Parse<SupplyEnum>(otherExchange.Supply),
                            otherExchange.TradeVolume,
                            navigationFactor,
                            timeCost
                        ));
                    }
                }
            }
        });
        //}
        return tradeModels.ToList();
    }

    public async Task<List<SellModel>> GetSellModelsAsyncWithBurn2(List<string> systemSymbols, string originWaypointSymbol, int fuelMax, int fuelCurrent)
    {
        var tradeModels = await GetTradeModelsWithCacheAsync();

        var localSellModels = tradeModels.Where(tm => systemSymbols.Any(s => systemSymbols.Any(s => s.StartsWith(tm.ImportWaypointSymbol))))
            .Select(tm => new SellModel(tm.TradeSymbol, tm.ImportWaypointSymbol, tm.ImportSellPrice, tm.ImportSupplyEnum, tm.ImportTradeVolume, 0, 0));
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(originWaypointSymbol));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        var pathModels = PathsService.BuildSystemPathWithCostWithBurn(waypoints, originWaypointSymbol, fuelMax, fuelCurrent);
        List<SellModel> tradeModelsWithOriginTimeCost = [];
        foreach (var localSellModel in localSellModels)
        {
            var pathModel = pathModels.Single(pm => pm.WaypointSymbol == localSellModel.WaypointSymbol);
            var newLocalSellModel = localSellModel with { TimeCost = localSellModel.TimeCost + pathModel.TimeCost };
            newLocalSellModel = newLocalSellModel with { NavigationFactor = NavigationFactor(newLocalSellModel.TimeCost) };
            tradeModelsWithOriginTimeCost.Add(newLocalSellModel);
        }

        var sellModelsWithOriginTimeCostGrouped = tradeModelsWithOriginTimeCost.GroupBy(tm => tm.TradeSymbol);
        var bestSellModelBySymbol = sellModelsWithOriginTimeCostGrouped.Select(tmg => tmg.OrderBy(tm => tm.TimeCost).First()).ToList();
        return bestSellModelBySymbol.OrderBy(tm => tm.TimeCost).ToList();
    }

    public static decimal GetSellModelNavigationFactorWithBurn2(SellModel sellModel)
    {
        decimal score = sellModel.SellPrice;
        score *= SupplyFactorImportMuliplier(sellModel.SupplyEnum);
        score *= NavigationFactor(sellModel.TimeCost);
        return Math.Round(score, 0);
    }

    public static decimal GetTradeModelNavigationFactorWithBurn2(
        int exportBuyPrice,
        int importSellPrice,
        SupplyEnum exportSupplyEnum,
        SupplyEnum importSupplyEnum,
        int timeCost)
    {
        const decimal profitWeight = 0.5m;
        const decimal marginWeight = 0.5m;

        var profit = importSellPrice - exportBuyPrice;
        var marginPercent = (decimal)profit / exportBuyPrice;

        var score =
            (profitWeight * profit) +
            (marginWeight * marginPercent * 100); // scale percentage for balance

        score *= SupplyFactor(exportSupplyEnum, importSupplyEnum);
        score *= GetTimeCostFactorWithBurn2(timeCost);
        return Math.Round(score, 0);
    }

    private static decimal GetTimeCostFactorWithBurn2(int cost)
    {
        if (cost <= 200) return 1;
        if (cost <= 400) return .9m;
        if (cost <= 800) return .7m;
        if (cost <= 1200) return .5m;
        if (cost <= 2400) return .25m;
        return .1m;
    }
}

public record TradeModel(
    string TradeSymbol,
    string ExportWaypointSymbol,
    int ExportBuyPrice,
    SupplyEnum ExportSupplyEnum,
    int ExportTradeVolume,
    string ImportWaypointSymbol,
    int ImportSellPrice,
    SupplyEnum ImportSupplyEnum,
    int ImportTradeVolume,
    decimal NavigationFactor,
    int TimeCost
);

public record SellModel(
    string TradeSymbol,
    string WaypointSymbol,
    int SellPrice,
    SupplyEnum SupplyEnum,
    int TradeVolume,
    decimal NavigationFactor,
    int TimeCost
);