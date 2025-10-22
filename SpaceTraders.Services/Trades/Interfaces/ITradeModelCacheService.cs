using SpaceTraders.Models;

namespace SpaceTraders.Services.Trades.Interfaces;

public interface ITradeModelCacheService
{
    Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();
    Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync(int fuelMax, int fuelCurrent);
    Task SaveTradeModelsAsync(IReadOnlyList<Waypoint> waypoints, int fuelMax, int fuelCurrent);
}