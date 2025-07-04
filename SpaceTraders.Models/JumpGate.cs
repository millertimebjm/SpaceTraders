namespace SpaceTraders.Models;

public record JumpGate(
    string Symbol,
    IReadOnlyList<string> Connections
);