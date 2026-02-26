using SpaceTraders.Models;

namespace SpaceTraders.Services.Paths.Interfaces;

public interface IPathsService
{
    Task<Dictionary<string, ValueTuple<List<string>, int>>> BuildSystemPath(
        string originWaypoint,
        int fuelMax,
        int fuelCurrent);

    Task<Dictionary<string, ValueTuple<List<string>, int>>> BuildSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int fuelCurrent);

    Task<Dictionary<string, ValueTuple<List<string>, int>>> BuildSystemPathWithCost(
        List<Waypoint> waypoints,
        Waypoint currentWaypoint,
        int fuelMax,
        int startingFuel);

    Task<Dictionary<string, ValueTuple<List<string>, int>>> BuildSystemPathWithCostWithMemo(
        string originWaypoint,
        int fuelMax,
        int startingFuel);
}