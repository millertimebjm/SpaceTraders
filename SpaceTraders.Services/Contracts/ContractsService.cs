using SpaceTraders.Models;
using SpaceTraders.Models.Results;
using SpaceTraders.Services.Contracts.Interfaces;

namespace SpaceTraders.Services.Contracts;

public class ContractsService(
    IContractsApiService _contractsApiService,
    IContractsCacheService _contractsCacheService
) : IContractsService
{
    public async Task<IEnumerable<STContract>> GetAsync(bool refresh = false)
    {
        if (!refresh)
        {
            var cacheContracts = await _contractsCacheService.GetAsync();
            if (cacheContracts.Any()) return cacheContracts;
        }

        var contracts = await _contractsApiService.GetAsync();
        await _contractsCacheService.SetAsync(contracts);
        return contracts;
    }

    public async Task<STContract?> GetActiveAsync()
    {
        var cacheContracts = await _contractsCacheService.GetAsync();
        var activeCacheContract = cacheContracts.SingleOrDefault(c => c.Accepted && !c.Fulfilled);
        if (activeCacheContract is not null) return activeCacheContract; 
        
        var contracts = await _contractsApiService.GetAsync();
        await _contractsCacheService.SetAsync(contracts);
        return contracts.SingleOrDefault(c => c.Accepted && !c.Fulfilled);
    }

    public async Task<STContract> GetAsync(string contractId)
    {
        return await _contractsCacheService.GetAsync(contractId);
    }

    public async Task<ContractAcceptResult> AcceptAsync(string contractId)
    {
        var result = await _contractsApiService.AcceptAsync(contractId);
        await _contractsCacheService.SetAsync(result.Contract);
        return result;
    }

    public async Task<ContractFulfillResult> FulfillAsync(string contractId)
    {
        var result = await _contractsApiService.FulfillAsync(contractId);
        await _contractsCacheService.SetAsync(result.Contract);
        return result;
    }

    public async Task<ContractDeliverResult> DeliverAsync(string contractId,
        string shipSymbol,
        string tradeSymbol,
        int units)
    {
        var result = await _contractsApiService.DeliverAsync(contractId, shipSymbol, tradeSymbol, units);
        await _contractsCacheService.SetAsync(result.Contract);
        return result;
    }

    public async Task<ContractNegotiateResult> NegotiateAsync(string shipSymbol)
    {
        var result = await _contractsApiService.NegotiateAsync(shipSymbol);
        await _contractsCacheService.SetAsync(result.Contract);
        return result;
    }
}