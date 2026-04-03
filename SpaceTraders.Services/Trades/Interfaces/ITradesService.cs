using SpaceTraders.Models;

namespace SpaceTraders.Services.Trades;

public interface ITradesService
{
    // TradeModel? GetBestTrade(IReadOnlyList<TradeModel> trades);
    // TradeModel? GetAnyBestTrade(IReadOnlyList<TradeModel> trades);
    // SellModel? GetBestSellModel(IReadOnlyList<SellModel> sellModels);
    // IReadOnlyList<SellModel> BuildSellModel(
    //     IReadOnlyList<Waypoint> waypoints, Waypoint? originWaypoint = null, int? fuelMax = 0, int? fuelCurrent = 0);
    // // Task SaveTradeModelsAsync(IReadOnlyList<Waypoint> waypoints, int fuelMax, int fuelCurrent);
    // // Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();
    // //IReadOnlyList<TradeModel> GetBestOrderedTrades(IReadOnlyList<TradeModel> trades);
    // IReadOnlyList<TradeModel> GetBestOrderedTradesWithTravelCost(
    //     IReadOnlyList<TradeModel> trades);
    // Task<IReadOnlyList<TradeModel>> GetBestOrderedTradesWithTravelCost(
    //     string originWaypoint,
    //     int fuelMax,
    //     int fuelCurrent);
    // Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync(string waypointSymbol, int maxFuel, int currentFuel);
    // Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync(List<Waypoint> waypoints, string originWaypoint, int maxFuel, int currentFuel);
    // Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();
    // Task<IReadOnlyList<TradeModel>> GetBestOrderedTradesWithTravelCost();
    // Task BuildTradeModel();

    Task UpdateTradeModelAsync(string waypointSymbol, IReadOnlyList<TradeGood> tradeGoods);
    Task<List<TradeModel>> GetTradeModelsWithCacheAsync();
    Task<List<TradeModel>> GetTradeModelsAsyncWithBurn2(List<string> systemSymbols, string originWaypointSymbol, int fuelMax, int fuelCurrent, int distance = 1);
    Task<List<SellModel>> GetSellModelsAsyncWithBurn2(List<string> systemSymbols, string originWaypointSymbol, int fuelMax, int fuelCurrent);
    Task TradeModelRefreshIfNone(bool refresh = false);
    Task SaveTradeModelWithBurnAsync2ByMarketplaceSetup(Waypoint marketplaceWaypoint);
}