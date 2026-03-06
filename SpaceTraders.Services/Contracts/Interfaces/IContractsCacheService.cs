using SpaceTraders.Models;

namespace SpaceTraders.Services.Contracts.Interfaces;

public interface IContractsCacheService
{
    Task<IEnumerable<STContract>> GetAsync();
    Task<STContract> GetAsync(string id);
    Task SetAsync(STContract contract);
    Task SetAsync(IEnumerable<STContract> contracts);
}