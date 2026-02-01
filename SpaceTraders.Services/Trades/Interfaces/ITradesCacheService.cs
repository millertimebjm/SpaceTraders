using SpaceTraders.Models;

namespace SpaceTraders.Services.Trades.Interfaces;

public interface ITradesCacheService
{
    Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync();

    Task SaveTradeModelsAsync(IReadOnlyList<TradeModel> tradeModels);
}