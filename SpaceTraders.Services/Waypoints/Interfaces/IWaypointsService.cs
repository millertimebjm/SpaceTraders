using SpaceTraders.Models;

namespace SpaceTraders.Services.Waypoints.Interfaces;

public interface IWaypointsService
{
    Task<Waypoint> GetAsync(
        string waypointSymbol,
        bool refresh = false);
    Task<IEnumerable<Waypoint>> GetByTypeAsync(string waypointSymbol, string type);
    Task<IEnumerable<Waypoint>> GetByTraitAsync(string waypointSymbol, string trait);
}