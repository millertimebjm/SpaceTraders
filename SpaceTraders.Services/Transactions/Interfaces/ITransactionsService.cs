using SpaceTraders.Models;

namespace SpaceTraders.Services.Transactions.Interfaces;

public interface ITransactionsService
{
    Task SetAsync(MarketTransaction transaction);
    Task<IReadOnlyList<MarketTransaction>> GetAsync(string shipSymbol, int take = 200);
}