using SpaceTraders.Models;

namespace SpaceTraders.Services.Contracts.Interfaces;

public interface IContractsService
{
    Task<IEnumerable<STContract>> GetAsync();
    Task<STContract> AcceptAsync(string contractId);
}