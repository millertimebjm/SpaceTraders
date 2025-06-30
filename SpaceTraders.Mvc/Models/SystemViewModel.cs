using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

public record SystemViewModel(
    Task<STSystem> SystemTask,
    Task<Ship?> ShipTask,
    Task<Waypoint?> WaypointTask
);