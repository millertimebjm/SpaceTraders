namespace SpaceTraders.Models;

public record Waypoint(
    string Symbol,
    string SystemSymbol,
    string Type,
    int X,
    int Y,
    IReadOnlyList<Orbital> Orbitals,
    string Orbits
);