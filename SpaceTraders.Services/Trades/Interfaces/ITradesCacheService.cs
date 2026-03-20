using SpaceTraders.Models;

namespace SpaceTraders.Services.Trades.Interfaces;

public interface ITradesCacheService
{
    Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();
    Task SaveTradeModelsAsync(IReadOnlyList<TradeModel> tradeModels);
    Task UpdateExistingTradeModelsAsync(string waypointSymbol, IReadOnlyList<TradeGood> tradeGoods);
    Task<bool> AnyTradeModelAsync();
    Task<bool> AnyTradeModelAsync(string waypointSymbol);
    Task InsertNewTradeModelsAsync(List<TradeModel> tradeModels);
}