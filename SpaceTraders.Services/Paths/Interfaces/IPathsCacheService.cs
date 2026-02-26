using SpaceTraders.Models;

namespace SpaceTraders.Services.Paths.Interfaces;

public interface IPathsCacheService
{
    Task<Dictionary<string, ValueTuple<List<string>, int>>?> GetSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int startingFuel
    );

    Task SetSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int startingFuel,
        Dictionary<string, ValueTuple<List<string>, int>> systemPath
    );

    Task ClearAllCachedSystemPaths();

    public Task<decimal?> GetNavigationFactor(string exportSymbol, string importSymbol, int fuelMax, int fuelCurrent);

    public Task SetNavigationFactor(string exportSymbol, string importSymbol, int fuelMax, int fuelCurrent, decimal factor);
}