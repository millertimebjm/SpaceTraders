using SpaceTraders.Models;

namespace SpaceTraders.Services.Agents;

public interface IAgentFileCacheService
{
    Task<Agent> GetAsync();
    Task<Agent> SetAsync(Agent agent);
}