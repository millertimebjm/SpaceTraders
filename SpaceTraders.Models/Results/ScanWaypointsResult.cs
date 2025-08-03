namespace SpaceTraders.Models;

public record ScanWaypointsResult(
    Cooldown Cooldown,
    IReadOnlyList<Waypoint> Waypoints
);