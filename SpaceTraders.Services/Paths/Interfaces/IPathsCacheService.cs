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
}