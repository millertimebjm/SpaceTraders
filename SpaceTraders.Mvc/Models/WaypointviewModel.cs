using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

public record WaypointViewModel(
    Task<Waypoint> Waypoint,
    Task<Waypoint?> CurrentWaypoint,
    Task<Ship?> CurrentShip
);