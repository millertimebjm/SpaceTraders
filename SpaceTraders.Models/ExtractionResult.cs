namespace SpaceTraders.Models;

    public record ExtractionResult(
 Extraction Extraction,
 Cooldown Cooldown,
 Cargo Cargo,
 IReadOnlyList<Modifier> Modifiers,
 IReadOnlyList<Event> Events
    );

    public record Event(
 string Symbol,
 string Component,
 string Name,
 string Description
    );

    public record Extraction(
 string ShipSymbol,
 Yield Yield
    );

    public record Modifier(
 string Symbol,
 string Name,
 string Description
    );

    public record Yield(
 string Symbol,
 int Units
    );