namespace SpaceTraders.Models;

public record Waypoint(
    string Symbol,
    string Type,
    int X,
    int Y,
    IReadOnlyList<Orbital> Orbitals,
    string Orbits
);