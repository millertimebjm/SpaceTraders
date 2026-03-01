using SpaceTraders.Models;

namespace SpaceTraders.Services.Trades.Interfaces;

public interface ITradesCacheService
{
    Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();
    Task SaveTradeModelsAsync(IReadOnlyList<TradeModel> tradeModels);
    Task SaveTradeModelsAsync(IEnumerable<TradeModel> tradeModels, int fuelMax, int fuelCurrent);
    Task UpdateTradeModelAsync(string waypointSymbol, IReadOnlyList<TradeGood> tradeGoods);
    Task<bool> AnyTradeModelAsync(string waypointSymbol);
}