using SpaceTraders.Models;

namespace SpaceTraders.Services.Paths.Interfaces;

public interface IPathsService
{
    Task<Dictionary<Waypoint, ValueTuple<List<Waypoint>, int>>> BuildSystemPath(
        string originWaypoint,
        int fuelMax,
        int fuelCurrent);

    Task<Dictionary<Waypoint, ValueTuple<List<Waypoint>, int>>> BuildSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int fuelCurrent);

    Task<Dictionary<Waypoint, ValueTuple<List<Waypoint>, int>>> BuildSystemPathWithCost(
        List<Waypoint> waypoints,
        Waypoint currentWaypoint,
        int fuelMax,
        int startingFuel);

    Task<Dictionary<Waypoint, ValueTuple<List<Waypoint>, int>>> BuildSystemPathWithCostWithMemo(
        string originWaypoint,
        int fuelMax,
        int startingFuel);
}