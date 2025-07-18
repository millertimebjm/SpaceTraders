using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Marketplaces;

public class MarketplacesService : IMarketplacesService
{
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly IMongoCollectionFactory _collectionFactory;
    private readonly ILogger<MarketplacesService> _logger;

    public MarketplacesService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMongoCollectionFactory collectionFactory,
        ILogger<MarketplacesService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[$"SpaceTrader:" + ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[$"SpaceTrader:" + ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
        _collectionFactory = collectionFactory;
    }

    public async Task<Marketplace> GetAsync(string marketplaceWaypointSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/systems/{WaypointsService.ExtractSystemFromWaypoint(marketplaceWaypointSymbol)}/waypoints/{marketplaceWaypointSymbol}/market"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<Marketplace>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Shipyard not retrieved");
        return data.Datum;
    }

    public async Task<RefuelResponse> RefuelAsync(
        string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/refuel"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { symbol = InventoryEnum.FUEL.ToString() });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<RefuelResponse>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        return data.Datum;
    }

    public async Task<SellCargoResponse> SellAsync(
        string shipSymbol,
        string inventory,
        int units)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/sell"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { symbol = inventory, units });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<SellCargoResponse>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        return data.Datum;
    }

    public async Task<Cargo> SellAsync(
        string shipSymbol,
        InventoryEnum inventory,
        int units)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/sell"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { symbol = inventory.ToString(), units });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        return data.Datum.Cargo;
    }

    public async Task<PurchaseCargoResult> PurchaseAsync(
        string shipSymbol,
        string inventory,
        int units)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/purchase"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { symbol = inventory, units });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<PurchaseCargoResult>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Result not retrieved");
        return data.Datum;
    }



    public IReadOnlyList<TradeModel> BuildTradeModel(
        IReadOnlyList<Waypoint> marketplaceWaypoints)
    {
        marketplaceWaypoints = marketplaceWaypoints.Where(w => w.Marketplace is not null && w.Marketplace.TradeGoods is not null).ToList();
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
                            import.TradeVolume
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
                            import.TradeVolume
                        ));
                    }
                }
            }
        }
        return tradeModels;
    }


    // public TradeModel? GetBestTrade(IReadOnlyList<TradeModel> trades)
    // {
    //     var orderedTrades = trades
    //         .Where(t => t.ImportSellPrice > t.ExportBuyPrice) // only profitable trades
    //         .OrderByDescending(t =>
    //             (t.ImportSellPrice - t.ExportBuyPrice) *
    //             SupplyFactor(t.ExportSupplyEnum, t.ImportSupplyEnum)
    //         ).ToList();
    //     return orderedTrades.FirstOrDefault();
    // }

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

    private double SupplyFactor(SupplyEnum export, SupplyEnum import)
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

    public async Task SaveTradeModelsAsync(IReadOnlyList<Waypoint> waypoints)
    {
        var tradeModels = BuildTradeModel(waypoints);
        var collection = _collectionFactory.GetCollection<TradeModel>();
        await collection.DeleteManyAsync(FilterDefinition<TradeModel>.Empty);
        await collection.InsertManyAsync(tradeModels, new InsertManyOptions(), CancellationToken.None);
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
    int ImportTradeVolume
);

public record SellModel(
    string TradeSymbol,
    string WaypointSymbol,
    int SellPrice,
    SupplyEnum SupplyEnum,
    int TradeVolume
);