namespace SpaceTraders.Models;

public record STSystem(
    string Constellation,
    string Symbol,
    string SectorSymbol,
    string Type,
    int X,
    int Y,
    IReadOnlyList<Waypoint> Waypoints,
    IReadOnlyList<Faction> Factions,
    string Name
);
