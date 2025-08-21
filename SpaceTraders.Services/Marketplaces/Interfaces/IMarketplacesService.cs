using SpaceTraders.Models;

namespace SpaceTraders.Services.Marketplaces.Interfaces;

public interface IMarketplacesService
{
    Task<Marketplace> GetAsync(string marketplaceWaypointSymbol);
    Task<PurchaseCargoResult> PurchaseAsync(string shipSymbol, string symbol, int capacity);
    Task<RefuelResponse> RefuelAsync(string shipSymbol);
    Task<Cargo> SellAsync(string shipSymbol, InventoryEnum inventory, int units);
    Task<SellCargoResponse> SellAsync(string shipSymbol, string inventory, int units);
    Task<IReadOnlyList<TradeModel>> BuildTradeModel(
        IReadOnlyList<Waypoint> marketplaceWaypoints,
        int fuelMax,
        int fuelCurrent);
    TradeModel? GetBestTrade(IReadOnlyList<TradeModel> trades);
    TradeModel? GetAnyBestTrade(IReadOnlyList<TradeModel> trades);
    SellModel? GetBestSellModel(IReadOnlyList<SellModel> sellModels);
    IReadOnlyList<SellModel> BuildSellModel(
        IReadOnlyList<Waypoint> waypoints);
    Task SaveTradeModelsAsync(IReadOnlyList<Waypoint> waypoints, int fuelMax, int fuelCurrent);
    Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();
    IReadOnlyList<TradeModel> GetBestOrderedTrades(IReadOnlyList<TradeModel> trades);
    IReadOnlyList<TradeModel> GetBestOrderedTradesWithTravelCost(
        IReadOnlyList<TradeModel> trades);
}