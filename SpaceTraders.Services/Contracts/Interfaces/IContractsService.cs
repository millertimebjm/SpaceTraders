using SpaceTraders.Models;
using SpaceTraders.Models.Results;

namespace SpaceTraders.Services.Contracts.Interfaces;

public interface IContractsService
{
    Task<IEnumerable<STContract>> GetAsync(bool refresh = false);
    Task<STContract?> GetActiveAsync(bool refresh = false);
    Task<STContract> GetAsync(string contractId);
    Task<ContractAcceptResult> AcceptAsync(string contractId);
    Task<ContractFulfillResult> FulfillAsync(string contractId);
    Task<ContractDeliverResult> DeliverAsync(string contractId,
        string shipSymbol,
        string tradeSymbol,
        int units);
    Task<ContractNegotiateResult> NegotiateAsync(string shipSymbol);
    Task SetAsync(STContract contract);
}