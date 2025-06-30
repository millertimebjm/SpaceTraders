using SpaceTraders.Models;

namespace SpaceTraders.Services.Waypoints.Interfaces;

public interface IWaypointsApiService
{
    Task<Waypoint> GetAsync(string waypointSymbol);
    Task<IEnumerable<Waypoint>> GetByTypeAsync(string systemSymbol, string type);
    Task<IEnumerable<Waypoint>> GetByTraitAsync(string systemSymbol, string trait);
}