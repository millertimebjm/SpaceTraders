using SpaceTraders.Models;

namespace SpaceTraders.Services.Agents.Interfaces;

public interface IAgentsService
{
    Task<Agent> GetAsync(bool refresh = false);
    Task SetAsync(Agent agent);
}