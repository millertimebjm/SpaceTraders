using SpaceTraders.Models;

namespace SpaceTraders.Services.Systems.Interfaces;

public interface ISystemsCacheService
{
    Task<IReadOnlyList<STSystem>> GetAsync();
    Task<STSystem> GetAsync(string systemSymbol, bool refresh = false);
    Task SetAsync(STSystem system);
    Task SetAsync(Waypoint waypoint);
}