using SpaceTraders.Models;

namespace SpaceTraders.Services.Paths.Interfaces;

public interface IPathsCacheService
{
    Task<Dictionary<Waypoint, ValueTuple<List<Waypoint>, int>>?> GetSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int startingFuel
    );

    Task SetSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int startingFuel,
        Dictionary<Waypoint, ValueTuple<List<Waypoint>, int>> systemPath
    );

    public Task<decimal?> GetNavigationFactor(string exportSymbol, string importSymbol, int fuelMax, int fuelCurrent);

    public Task SetNavigationFactor(string exportSymbol, string importSymbol, int fuelMax, int fuelCurrent, decimal factor);
}