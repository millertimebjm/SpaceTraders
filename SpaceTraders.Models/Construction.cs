namespace SpaceTraders.Models;

public record Construction(
    string Symbol,
    IReadOnlyList<Material> Materials
);