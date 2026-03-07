using SpaceTraders.Models;

namespace SpaceTraders.Services.Trades;

public interface ITradesService
{
    TradeModel? GetBestTrade(IReadOnlyList<TradeModel> trades);
    TradeModel? GetAnyBestTrade(IReadOnlyList<TradeModel> trades);
    SellModel? GetBestSellModel(IReadOnlyList<SellModel> sellModels);
    IReadOnlyList<SellModel> BuildSellModel(
        IReadOnlyList<Waypoint> waypoints);
    // Task SaveTradeModelsAsync(IReadOnlyList<Waypoint> waypoints, int fuelMax, int fuelCurrent);
    // Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();
    IReadOnlyList<TradeModel> GetBestOrderedTrades(IReadOnlyList<TradeModel> trades);
    Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();
    Task BuildTradeModel();
}