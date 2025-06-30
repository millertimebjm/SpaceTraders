using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

public record WaypointsViewModel(
    Task<IEnumerable<Waypoint>> Waypoints,
    Task<Waypoint?> CurrentWaypoint,
    Task<Ship?> CurrentShip,
    Task<STSystem> System
);