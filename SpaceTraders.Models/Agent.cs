namespace SpaceTraders.Models;

public record Agent(
    string Symbol,
    string? AccountId,
    string Headquarters,
    long Credits,
    string StartingFaction,
    int ShipCount);
