using SpaceTraders.Models;

namespace SpaceTraders.Services.Trades.Interfaces;

public interface ITradesCacheService
{
    Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();
    Task SaveTradeModelsAsync(IReadOnlyList<TradeModel> tradeModels);
    Task<IEnumerable<TradeModel>> GetTradeModelsAsync(int fuelMax, int fuelCurrent);
    Task SaveTradeModelsAsync(IEnumerable<TradeModel> tradeModels, int fuelMax, int fuelCurrent);
}