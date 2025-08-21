using SpaceTraders.Models;

namespace SpaceTraders.Services.Paths.Interfaces;

public interface IPathsService
{
    Task<Dictionary<Waypoint, (List<Waypoint>, int)>> BuildSystemPath(
        string originWaypoint,
        int fuelMax,
        int fuelCurrent);

    Task<Dictionary<Waypoint, (List<Waypoint>, int)>> BuildSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int fuelCurrent);

    Task<Dictionary<Waypoint, (List<Waypoint>, int)>> BuildSystemPathWithCostWithMemo(
        string originWaypoint,
        int fuelMax,
        int startingFuel);
}