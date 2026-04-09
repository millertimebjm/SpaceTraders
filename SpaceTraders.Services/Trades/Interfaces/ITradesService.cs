using SpaceTraders.Models;

namespace SpaceTraders.Services.Trades;

public interface ITradesService
{
    Task UpdateTradeModelAsync(string waypointSymbol, IReadOnlyList<TradeGood> tradeGoods);
    Task<List<TradeModel>> GetTradeModelsWithCacheAsync();
    Task<List<TradeModel>> GetTradeModelsAsyncWithBurn2Grouped(List<string> systemSymbols, string originWaypointSymbol, int fuelMax, int fuelCurrent, int distance = 1);
    Task<List<TradeModel>> GetTradeModelsAsyncWithBurn2(List<string> systemSymbols, string originWaypointSymbol, int fuelMax, int fuelCurrent, int distance = 1);

    Task<List<SellModel>> GetSellModelsAsyncWithBurn2(List<string> systemSymbols, string originWaypointSymbol, int fuelMax, int fuelCurrent);
    Task TradeModelRefreshIfNone(bool refresh = false);
    Task SaveTradeModelWithBurnAsync2ByMarketplaceSetup(Waypoint marketplaceWaypoint);
}