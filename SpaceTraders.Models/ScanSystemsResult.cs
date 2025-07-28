namespace SpaceTraders.Models;

public record ScanSystemsResult(
    Cooldown Cooldown,
    IReadOnlyList<STSystem> Systems
);