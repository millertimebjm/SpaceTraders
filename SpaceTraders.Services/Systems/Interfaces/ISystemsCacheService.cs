using SpaceTraders.Models;

namespace SpaceTraders.Services.Systems.Interfaces;

public interface ISystemsCacheService
{
    Task<STSystem> GetAsync(string systemSymbol, bool refresh = false);
    Task SetAsync(STSystem system);
}