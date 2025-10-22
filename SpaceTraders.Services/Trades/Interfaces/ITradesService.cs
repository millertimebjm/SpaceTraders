using SpaceTraders.Models;

namespace SpaceTraders.Services.Trades;

public interface ITradesService
{
    Task<IReadOnlyList<TradeModel>> BuildTradeModel(
        IReadOnlyList<Waypoint> marketplaceWaypoints,
        int fuelMax,
        int fuelCurrent);
    TradeModel? GetBestTrade(IReadOnlyList<TradeModel> trades);
    TradeModel? GetAnyBestTrade(IReadOnlyList<TradeModel> trades);
    SellModel? GetBestSellModel(IReadOnlyList<SellModel> sellModels);
    IReadOnlyList<SellModel> BuildSellModel(
        IReadOnlyList<Waypoint> waypoints);
    IReadOnlyList<TradeModel> GetBestOrderedTrades(IReadOnlyList<TradeModel> trades);
    IReadOnlyList<TradeModel> GetBestOrderedTradesWithTravelCost(
        IReadOnlyList<TradeModel> trades);
}