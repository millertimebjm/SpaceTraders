using SpaceTraders.Models;

namespace SpaceTraders.Services.Waypoints.Interfaces;

public interface IWaypointsService
{
    Task<Waypoint> GetAsync(string waypointSymbol);
    Task<IEnumerable<Waypoint>> GetByTypeAsync(string waypointSymbol, string type);
}