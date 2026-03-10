namespace SpaceTraders.Models.Results;

    public record SiphonResult(
 Siphon Siphon,
 Cooldown Cooldown,
 Cargo Cargo,
 IReadOnlyList<Event> Events
    );

    public record Siphon(
 string ShipSymbol,
 Yield Yield
    );