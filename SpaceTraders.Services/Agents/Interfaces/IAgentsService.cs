using SpaceTraders.Models;

namespace SpaceTraders.Services.Agents.Interfaces;

public interface IAgentsService
{
    Task<Agent> GetAsync();
}