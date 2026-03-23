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

    Task<(decimal? NavigationFactor, int? TimeCost)> GetNavigationFactor(string exportSymbol, string importSymbol, int fuelMax, int fuelCurrent);
    //Task SetNavigationFactor(string exportSymbol, string importSymbol, int fuelMax, int fuelCurrent, decimal factor, int timeCost);
    Task<List<PathModelWithBurn>?> GetMemoizeSystemTravel(string key);
    Task SetMemoizeSystemTravel(string key, List<PathModelWithBurn> systemPath);
}