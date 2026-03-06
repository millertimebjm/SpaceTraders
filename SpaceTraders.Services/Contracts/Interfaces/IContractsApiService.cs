using SpaceTraders.Models;
using SpaceTraders.Models.Results;

namespace SpaceTraders.Services.Contracts.Interfaces;

public interface IContractsApiService
{
    Task<IEnumerable<STContract>> GetAsync();
    Task<STContract?> GetActiveAsync();
    Task<ContractAcceptResult> AcceptAsync(string contractId);
    Task<ContractFulfillResult> FulfillAsync(string contractId);
    Task<ContractDeliverResult> DeliverAsync(
        string contractId,
        string shipSymbol,
        string tradeSymbol,
        int units);
    Task<ContractNegotiateResult> NegotiateAsync(string shipSymbol);
}