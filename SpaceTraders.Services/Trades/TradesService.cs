using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Paths.Interfaces;

namespace SpaceTraders.Services.Trades;

public class TradesService : ITradesService
{
    private readonly IPathsService _pathsService;
    private readonly IMongoCollectionFactory _collectionFactory;
    public TradesService(
        IPathsService pathsService,
        IMongoCollectionFactory collectionFactory)
    {
        _pathsService = pathsService;
        _collectionFactory = collectionFactory;
    }

    public async Task<IReadOnlyList<TradeModel>> BuildTradeModel(
        IReadOnlyList<Waypoint> waypoints,
        int fuelMax,
        int fuelCurrent)
    {
        var marketplaceWaypoints = waypoints.Where(w => w.Marketplace is not null && w.Marketplace.TradeGoods is not null).ToList();
        List<TradeModel> tradeModels = new();
        foreach (var marketplaceWaypointExport in marketplaceWaypoints.Where(w => w.Marketplace.Exports.Any()).ToList())
        {
            foreach (var export in marketplaceWaypointExport.Marketplace.TradeGoods.Where(tg => tg.Type == "EXPORT").ToList())
            {
                foreach (var marketplaceWaypointImport in marketplaceWaypoints.Where(w => w.Marketplace.Imports.Any()).ToList())
                {
                    foreach (var import in marketplaceWaypointImport.Marketplace.TradeGoods.Where(tg => tg.Type == "IMPORT" && tg.Symbol == export.Symbol))
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
                            await GetNavigationFactor(marketplaceWaypointExport.Symbol, marketplaceWaypointImport.Symbol, fuelMax, fuelCurrent)
                        ));
                    }
                }
                foreach (var marketplaceWaypointExchange in marketplaceWaypoints.Where(w => w.Marketplace.Exchange.Any()).ToList())
                {
                    foreach (var import in marketplaceWaypointExchange.Marketplace.TradeGoods.Where(tg => tg.Type == "EXCHANGE" && tg.Symbol == export.Symbol))
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
                            await GetNavigationFactor(marketplaceWaypointExport.Symbol, marketplaceWaypointExchange.Symbol, fuelMax, fuelCurrent)
                        ));
                    }
                }
            }
        }
        return tradeModels;
    }

    public async Task<double> GetNavigationFactor(string exportSymbol, string importSymbol, int fuelMax, int fuelCurrent)
    {
        var paths = await _pathsService.BuildSystemPathWithCost(exportSymbol, fuelMax, fuelCurrent);
        var path = paths.Single(p => p.Key.Symbol == importSymbol);
        return NavigationFactor(path.Value.Item2);
    }

    public IReadOnlyList<TradeModel> GetBestOrderedTrades(IReadOnlyList<TradeModel> trades)
    {
        const double profitWeight = 0.5;
        const double marginWeight = 0.5;

        var orderedTrades = trades
            .Where(t => t.ImportSellPrice > t.ExportBuyPrice)
            .OrderByDescending(t =>
            {
                var profit = t.ImportSellPrice - t.ExportBuyPrice;
                var marginPercent = (double)profit / t.ExportBuyPrice;

                var score =
                    (profitWeight * profit) +
                    (marginWeight * marginPercent * 100); // scale percentage for balance

                return score * SupplyFactor(t.ExportSupplyEnum, t.ImportSupplyEnum);
            })
            .ToList();
        return orderedTrades;
    }

    public IReadOnlyList<TradeModel> GetBestOrderedTradesWithTravelCost(
        IReadOnlyList<TradeModel> trades)
    {
        const double profitWeight = 0.5;
        const double marginWeight = 0.5;

        var orderedTrades = trades
            .Where(t => t.ImportSellPrice > t.ExportBuyPrice)
            .OrderByDescending(t =>
            {
                var profit = t.ImportSellPrice - t.ExportBuyPrice;
                var marginPercent = (double)profit / t.ExportBuyPrice;

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

    private static double SupplyFactor(SupplyEnum export, SupplyEnum import)
    {
        // Assign numeric values to supply levels (tune these based on game logic)
        double exportMultiplier = export switch
        {
            SupplyEnum.ABUNDANT => 5,
            SupplyEnum.HIGH => 3,
            SupplyEnum.MODERATE => 1,
            SupplyEnum.LIMITED => 0.5,
            SupplyEnum.SCARCE => 0.3,
            _ => 0
        };

        double importMultiplier = import switch
        {
            SupplyEnum.ABUNDANT => 0.3,
            SupplyEnum.HIGH => 0.5,
            SupplyEnum.MODERATE => 0.8,
            SupplyEnum.LIMITED => 1.0,
            SupplyEnum.SCARCE => 1.2,
            _ => 0
        };

        return exportMultiplier * importMultiplier;
    }

    private static double NavigationFactor(int cost)
    {
        if (cost <= 400) return 1;
        if (cost <= 800) return .85;
        if (cost <= 1600) return .7;
        if (cost <= 3200) return .55;
        if (cost <= 6400) return .4;
        return .25;
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

    public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync()
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        var projection = Builders<TradeModel>.Projection.Exclude("_id");

        return await collection
            .Find(FilterDefinition<TradeModel>.Empty)
            .Project<TradeModel>(projection)
            .ToListAsync();
    }

    public async Task SaveTradeModelsAsync(IReadOnlyList<Waypoint> waypoints, int fuelMax, int fuelCurrent)
    {
        var tradeModels = await BuildTradeModel(waypoints, fuelMax, fuelCurrent);
        if (tradeModels.Any())
        {
            var collection = _collectionFactory.GetCollection<TradeModel>();
            await collection.DeleteManyAsync(FilterDefinition<TradeModel>.Empty);
            await collection.InsertManyAsync(tradeModels, new InsertManyOptions(), CancellationToken.None);
        }
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
    double NavigationFactor
);

public record SellModel(
    string TradeSymbol,
    string WaypointSymbol,
    int SellPrice,
    SupplyEnum SupplyEnum,
    int TradeVolume
);