using SpaceTraders.Models;

namespace SpaceTraders.Services.Waypoints.Interfaces;

public interface IWaypointsCacheService
{
    Task<Waypoint?> GetAsync(string waypointSymbol);
    Task SetAsync(Waypoint waypoint);
    Task<IEnumerable<Waypoint>?> GetByTypeAsync(string waypointSymbol, string type);
    Task<IEnumerable<Waypoint>?> GetByTraitAsync(string waypointSymbol, string trait);
}