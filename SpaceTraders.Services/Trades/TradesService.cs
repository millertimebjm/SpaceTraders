using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Trades;

public class TradesService(
    ILogger<TradesService> _logger,
    IPathsService _pathsService,
    IPathsCacheService _pathsCacheService,
    ITradesCacheService _tradesCacheService,
    ISystemsService _systemsService,
    IAgentsService _agentsService
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
        await BuildTradeModel();
    }

    public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync()
    {
        return await _tradesCacheService.GetTradeModelsAsync();
    }

    public async Task BuildTradeModel()
    {
        var agent = await _agentsService.GetAsync();
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters));
        var waypoints = traversableSystems.SelectMany(ts => ts.Waypoints).ToList();
        var newTradeModels = await BuildTradeModel(waypoints, 600, 600);
        await _tradesCacheService.SaveTradeModelsAsync(newTradeModels);
    }

    private async Task<IReadOnlyList<TradeModel>> BuildTradeModel(
        IReadOnlyList<Waypoint> waypoints,
        int fuelMax,
        int fuelCurrent)
    {
        var marketplaceWaypoints = waypoints.Where(w => w.Marketplace is not null && w.Marketplace.TradeGoods is not null).ToList();
        ConcurrentBag<TradeModel> tradeModels = new();
        var marketplaceWaypointExports = marketplaceWaypoints.Where(w => w.Marketplace.Exports.Any()).ToList();
        await Parallel.ForEachAsync(marketplaceWaypointExports, async (marketplaceWaypointExport, CancellationToken) =>
        //foreach (var marketplaceWaypointExport in marketplaceWaypointExports)
        {
            var exports = marketplaceWaypointExport.Marketplace.TradeGoods.Where(tg => tg.Type == "EXPORT").ToList();
            foreach (var export in exports)
            {
                var imports = marketplaceWaypoints.Where(w => w.Marketplace.Imports.Any()).ToList();
                foreach (var marketplaceWaypointImport in imports)
                {
                    var navigationFactor = await GetNavigationFactor(waypoints, marketplaceWaypointExport, marketplaceWaypointImport.Symbol, fuelMax, fuelCurrent);
                    var marketplaceWaypointImports = marketplaceWaypointImport.Marketplace.TradeGoods.Where(tg => tg.Type == "IMPORT" && tg.Symbol == export.Symbol).ToList();
                    foreach (var import in marketplaceWaypointImports)
                    {
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
                            navigationFactor
                        ));
                    }
                }
                var exchangeMarketplaceWaypoints = marketplaceWaypoints.Where(w => w.Marketplace.Exchange.Any()).ToList();
                foreach (var marketplaceWaypointExchange in exchangeMarketplaceWaypoints)
                {
                    var navigationFactor = await GetNavigationFactor(waypoints, marketplaceWaypointExport, marketplaceWaypointExchange.Symbol, fuelMax, fuelCurrent);
                    var tradeGoodImports = marketplaceWaypointExchange.Marketplace.TradeGoods.Where(tg => tg.Type == "EXCHANGE" && tg.Symbol == export.Symbol);
                    foreach (var import in tradeGoodImports)
                    {
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
                            navigationFactor
                        ));
                    }
                }
            }
        });
        //}
        return tradeModels.ToList();
    }

    public async Task<decimal> GetNavigationFactor(IReadOnlyList<Waypoint> waypoints, Waypoint exportWaypoint, string importSymbol, int fuelMax, int fuelCurrent)
    {
        var navigationFactorCache = await _pathsCacheService.GetNavigationFactor(exportWaypoint.Symbol, importSymbol, fuelMax, fuelCurrent);
        if (navigationFactorCache is not null) return navigationFactorCache.Value; 

        try
        {
            var paths = await _pathsService.BuildSystemPathWithCost(waypoints.ToList(), exportWaypoint, fuelMax, fuelCurrent);
            var path = paths.Single(p => p.Key == importSymbol);
            var navigationFactor = NavigationFactor(path.Value.Item2);
            await _pathsCacheService.SetNavigationFactor(exportWaypoint.Symbol, importSymbol, fuelMax, fuelCurrent, navigationFactor);
            return navigationFactor;
        }
        catch(Exception ex)
        {
            _logger.LogError("Paths were not updated because a new system became available.");
            await _pathsCacheService.ClearAllCachedSystemPaths();

            var paths = await _pathsService.BuildSystemPathWithCost(waypoints.ToList(), exportWaypoint, fuelMax, fuelCurrent);
            var path = paths.Single(p => p.Key == importSymbol);
            var navigationFactor = NavigationFactor(path.Value.Item2);
            await _pathsCacheService.SetNavigationFactor(exportWaypoint.Symbol, importSymbol, fuelMax, fuelCurrent, navigationFactor);
            return navigationFactor;
        }
    }

    public IReadOnlyList<TradeModel> GetBestOrderedTrades(IReadOnlyList<TradeModel> trades)
    {
        const decimal profitWeight = 0.5m;
        const decimal marginWeight = 0.5m;

        var orderedTrades = trades
            .Where(t => t.ImportSellPrice > t.ExportBuyPrice)
            .OrderByDescending(t =>
            {
                var profit = t.ImportSellPrice - t.ExportBuyPrice;
                var marginPercent = (decimal)profit / t.ExportBuyPrice;

                var score =
                    (profitWeight * profit) +
                    (marginWeight * marginPercent * 100); // scale percentage for balance

                return score * SupplyFactor(t.ExportSupplyEnum, t.ImportSupplyEnum);
            })
            .ThenBy(t => t.ExportWaypointSymbol)
            .ThenBy(t => t.ImportWaypointSymbol)
            .ToList();
        return orderedTrades;
    }

    public IReadOnlyList<TradeModel> GetBestOrderedTradesWithTravelCost(
        IReadOnlyList<TradeModel> trades)
    {
        const decimal profitWeight = 0.5m;
        const decimal marginWeight = 0.5m;

        var orderedTrades = trades
            .Where(t => t.ImportSellPrice > t.ExportBuyPrice)
            .OrderByDescending(t =>
            {
                var profit = t.ImportSellPrice - t.ExportBuyPrice;
                var marginPercent = (decimal)profit / t.ExportBuyPrice;

                var score =
                    (profitWeight * profit) +
                    (marginWeight * marginPercent * 100); // scale percentage for balance

                score = score * SupplyFactor(t.ExportSupplyEnum, t.ImportSupplyEnum);
                score = score * t.NavigationFactor;
                return score;
            })
            .ToList();
        return orderedTrades;
    }

    public TradeModel? GetBestTrade(IReadOnlyList<TradeModel> trades)
    {
        var orderedTrades = GetBestOrderedTrades(trades);
        return orderedTrades.FirstOrDefault();
    }

    public TradeModel? GetAnyBestTrade(IReadOnlyList<TradeModel> trades)
    {
        var orderedTrades = trades
            .OrderByDescending(t =>
                (t.ImportSellPrice - t.ExportBuyPrice) *
                SupplyFactor(t.ExportSupplyEnum, t.ImportSupplyEnum)
            ).ToList();
        return orderedTrades.FirstOrDefault();
    }

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

        decimal importMultiplier = import switch
        {
            SupplyEnum.ABUNDANT => 0.3m,
            SupplyEnum.HIGH => 0.5m,
            SupplyEnum.MODERATE => 0.8m,
            SupplyEnum.LIMITED => 1.0m,
            SupplyEnum.SCARCE => 1.2m,
            _ => 0
        };

        return exportMultiplier * importMultiplier;
    }

    private static decimal NavigationFactor(int cost)
    {
        if (cost <= 400) return 1;
        if (cost <= 800) return .85m;
        if (cost <= 1600) return .7m;
        if (cost <= 3200) return .55m;
        if (cost <= 6400) return .4m;
        return .25m;
    }

    public IReadOnlyList<SellModel> BuildSellModel(
        IReadOnlyList<Waypoint> waypoints)
    {
        var marketplaceWaypoints = waypoints.Where(w => w.Marketplace is not null && w.Marketplace.TradeGoods is not null).ToList();
        List<SellModel> sellModels = new();
        foreach (var marketplaceWaypoint in marketplaceWaypoints)
        {
            foreach (var tradeGood in marketplaceWaypoint.Marketplace.TradeGoods)
            {
                sellModels.Add(new SellModel(
                    tradeGood.Symbol,
                    marketplaceWaypoint.Symbol,
                    tradeGood.SellPrice,
                    Enum.Parse<SupplyEnum>(tradeGood.Supply),
                    tradeGood.TradeVolume
                ));
            }
        }
        return sellModels;
    }
    public SellModel? GetBestSellModel(IReadOnlyList<SellModel> sellModels)
    {
        var orderedTrades = sellModels
            .OrderByDescending(m =>
                m.SellPrice
            ).ToList();
        return orderedTrades.FirstOrDefault();
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
    decimal NavigationFactor
);

public record SellModel(
    string TradeSymbol,
    string WaypointSymbol,
    int SellPrice,
    SupplyEnum SupplyEnum,
    int TradeVolume
);