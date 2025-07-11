using SpaceTraders.Models;

namespace SpaceTraders.Services.Contracts.Interfaces;

public interface IContractsService
{
    Task<IEnumerable<STContract>> GetAsync();
    Task<STContract?> GetActiveAsync();
    Task<STContract> GetAsync(string contractId);
    Task<STContract> AcceptAsync(string contractId);
    Task FulfillAsync(string contractId);
    Task DeliverAsync(string contractId,
        string shipSymbol,
        string tradeSymbol,
        int units);
    Task NegotiateAsync(string shipSymbol);
}