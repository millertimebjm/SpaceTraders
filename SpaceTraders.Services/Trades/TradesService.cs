using System.Collections.Concurrent;
using MongoDB.Driver;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades.Interfaces;

namespace SpaceTraders.Services.Trades;

public class TradesService(
    ITradesCacheService _tradesCacheService,
    ISystemsService _systemsService
) : ITradesService
{
    public async Task UpdateTradeModelAsync(string waypointSymbol, IReadOnlyList<TradeGood> tradeGoods)
    {
        var hasTradeGoods = await _tradesCacheService.AnyTradeModelAsync(waypointSymbol);
        if (hasTradeGoods)
        {
            await _tradesCacheService.UpdateExistingTradeModelsAsync(waypointSymbol, tradeGoods);
            return;
        }
        var tradeModels = await _tradesCacheService.GetTradeModelsAsync();
        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var pathModels = PathsService.BuildSystemPathWithCostWithBurn(waypoints, waypointSymbol, 600, 600);

        List<TradeModel> newTradeModels = [];
        var exports = tradeGoods.Where(tg => (tg.Type == TradeGoodTypeEnum.EXPORT.ToString() || tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString()) && tg.Symbol != TradeSymbolsEnum.FUEL.ToString() && tg.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString()).ToList();
        foreach (var export in exports)
        {
            foreach (var tradeModel in tradeModels.Where(tm => tm.TradeSymbol == export.Symbol).ToList())
            {
                if (export.PurchasePrice > export.SellPrice) continue;
                var newTradeModel = tradeModel with
                {
                    ExportWaypointSymbol = waypointSymbol,
                    ExportBuyPrice = export.PurchasePrice,
                    ExportSupplyEnum = Enum.Parse<SupplyEnum>(export.Supply),
                    ExportTradeVolume = export.TradeVolume,
                    NavigationFactor = 0,
                    TimeCost = pathModels.Single(pm => pm.WaypointSymbol == tradeModel.ImportWaypointSymbol).TimeCost,
                };
                newTradeModel = newTradeModel with { NavigationFactor = GetTradeModelNavigationFactorWithBurn2(newTradeModel.ExportBuyPrice, newTradeModel.ImportSellPrice, newTradeModel.ExportSupplyEnum, newTradeModel.ImportSupplyEnum, newTradeModel.TimeCost) };
                newTradeModels.Add(newTradeModel);
            }
        }
        var imports = tradeGoods.Where(tg => (tg.Type == TradeGoodTypeEnum.IMPORT.ToString() || tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString()) && tg.Symbol != TradeSymbolsEnum.FUEL.ToString() && tg.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString()).ToList();
        foreach (var import in imports)
        {
            foreach (var tradeModel in tradeModels.Where(tm => tm.TradeSymbol == import.Symbol).ToList())
            {
                if (tradeModel.ExportBuyPrice > import.SellPrice) continue;
                var newTradeModel = tradeModel with
                {
                    ImportWaypointSymbol = waypointSymbol,
                    ImportSellPrice = import.SellPrice,
                    ImportSupplyEnum = Enum.Parse<SupplyEnum>(import.Supply),
                    ImportTradeVolume = import.TradeVolume,
                    NavigationFactor = 0,
                    TimeCost = pathModels.Single(pm => pm.WaypointSymbol == tradeModel.ImportWaypointSymbol).TimeCost,
                };
                newTradeModel = newTradeModel with { NavigationFactor = GetTradeModelNavigationFactorWithBurn2(newTradeModel.ExportBuyPrice, newTradeModel.ImportSellPrice, newTradeModel.ExportSupplyEnum, newTradeModel.ImportSupplyEnum, newTradeModel.TimeCost) };
                newTradeModels.Add(newTradeModel);
            }
        }
    }

    private static decimal SupplyFactor(SupplyEnum export, SupplyEnum import)
    {
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

    public async Task TradeModelRefreshIfNone()
    {
        if (!await _tradesCacheService.AnyTradeModelAsync())
        {
            var tradeModels = await BuildTradeModelWithBurnAsync2();
            await _tradesCacheService.SaveTradeModelsAsync(tradeModels);
        }
    }

    public async Task<List<TradeModel>> GetTradeModelsAsyncWithBurn2(List<string> systemSymbols, string originWaypointSymbol, int fuelMax, int fuelCurrent)
    {
        var tradeModels = await GetTradeModelsWithCacheAsync();

        var localTradeModels = tradeModels.Where(tm => systemSymbols.Any(s => tm.ExportWaypointSymbol.StartsWith(s)) && systemSymbols.Any(s => tm.ImportWaypointSymbol.StartsWith(s))).ToList();
        var systems = await _systemsService.GetAsync();
        var systemsIncluded = systems.Where(s => systemSymbols.Contains(s.Symbol));
        var waypoints = systemsIncluded.SelectMany(s => s.Waypoints).ToList();
        var pathModels = PathsService.BuildSystemPathWithCostWithBurn(waypoints, originWaypointSymbol, fuelMax, fuelCurrent);
        List<TradeModel> tradeModelsWithOriginTimeCost = [];
        foreach (var localTradeModel in localTradeModels)
        {
            var pathModel = pathModels.Single(pm => pm.WaypointSymbol == localTradeModel.ExportWaypointSymbol);
            var newLocalTradeModel = localTradeModel with { TimeCost = localTradeModel.TimeCost + pathModel.TimeCost };
            newLocalTradeModel = newLocalTradeModel with { NavigationFactor = GetTradeModelNavigationFactorWithBurn2(newLocalTradeModel.ExportBuyPrice, newLocalTradeModel.ImportSellPrice, newLocalTradeModel.ExportSupplyEnum, newLocalTradeModel.ImportSupplyEnum, newLocalTradeModel.TimeCost) };
            tradeModelsWithOriginTimeCost.Add(newLocalTradeModel);
        }

        var tradeModelsWithOriginTimeCostGrouped = tradeModelsWithOriginTimeCost.GroupBy(tm => tm.TradeSymbol);
        var bestTradeModelBySymbol = tradeModelsWithOriginTimeCostGrouped.Select(tmg => tmg.OrderByDescending(tm => tm.NavigationFactor).First()).ToList();
        return bestTradeModelBySymbol.OrderByDescending(tm => tm.TimeCost).ToList();
    }

    private async Task<List<TradeModel>> BuildTradeModelWithBurnAsync2()
    {
        int fuelMax = 600;
        int fuelCurrent = 600;
        var systems = await _systemsService.GetAsync();
        var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        var marketplaceWaypoints = waypoints.Where(w => w.Marketplace is not null && w.Marketplace.TradeGoods is not null).ToList();
        ConcurrentBag<TradeModel> tradeModels = [];
        var marketplaceWaypointExports = marketplaceWaypoints.Where(w => w.Marketplace.Exports.Any() || w.Marketplace.Exchange.Any()).ToList();

        await Parallel.ForEachAsync(marketplaceWaypointExports, async (marketplaceWaypointExport, CancellationToken) =>
        //foreach (var marketplaceWaypointExport in marketplaceWaypointExports)
        {
            var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints, marketplaceWaypointExport.Symbol, 600, 600);
            var exports = marketplaceWaypointExport.Marketplace.TradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXPORT.ToString() && tg.Symbol != TradeSymbolsEnum.FUEL.ToString() && tg.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString()).ToList();
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

        var localSellModels = tradeModels
            .Where(tm => systemSymbols.Any(s => systemSymbols.Any(s => tm.ImportWaypointSymbol.StartsWith(s))))
            .Select(tm => new SellModel(tm.TradeSymbol, tm.ImportWaypointSymbol, tm.ImportSellPrice, tm.ImportSupplyEnum, tm.ImportTradeVolume, 0, 0))
            .ToList();
        var systems = await _systemsService.GetAsync();
        var systemsIncluded = systems.Where(s => systemSymbols.Contains(s.Symbol)).ToList();
        var waypoints = systemsIncluded.SelectMany(s => s.Waypoints).ToList();
        var pathModels = PathsService.BuildSystemPathWithCostWithBurn(waypoints, originWaypointSymbol, fuelMax, fuelCurrent);
        List<SellModel> tradeModelsWithOriginTimeCost = [];
        foreach (var localSellModel in localSellModels)
        {
            var pathModel = pathModels.Single(pm => pm.WaypointSymbol == localSellModel.WaypointSymbol);
            var newLocalSellModel = localSellModel with { TimeCost = localSellModel.TimeCost + pathModel.TimeCost };
            newLocalSellModel = newLocalSellModel with { NavigationFactor = GetSellModelNavigationFactorWithBurn2(newLocalSellModel.SellPrice, newLocalSellModel.SupplyEnum, newLocalSellModel.TimeCost) };
            tradeModelsWithOriginTimeCost.Add(newLocalSellModel);
        }

        var sellModelsWithOriginTimeCostGrouped = tradeModelsWithOriginTimeCost.GroupBy(tm => tm.TradeSymbol);
        var bestSellModelBySymbol = sellModelsWithOriginTimeCostGrouped.Select(tmg => tmg.OrderBy(tm => tm.TimeCost).First()).ToList();
        return bestSellModelBySymbol.OrderByDescending(tm => tm.NavigationFactor).ToList();
    }

    public static decimal GetSellModelNavigationFactorWithBurn2(int sellPrice, SupplyEnum supplyEnum, int timeCost)
    {
        decimal score = sellPrice;
        score *= SupplyFactorImportMuliplier(supplyEnum);
        score *= NavigationFactor(timeCost);
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