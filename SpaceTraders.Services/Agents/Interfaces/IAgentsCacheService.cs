using SpaceTraders.Models;

namespace SpaceTraders.Services.Agents.Interfaces;

public interface IAgentsCacheService
{
    Task<Agent?> GetAsync();
    Task SetAsync(Agent agent);
}