using System.Text.Json.Serialization;

namespace SpaceTraders.Models;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public record Crew(
 int Required,
 int Capacity
    );

    public record Engine(
 string Symbol,
 string Name,
 decimal Condition,
 int Integrity,
 string Description,
 int Speed,
 Requirements Requirements,
 int Quality
    );

    public record Frame(
 string Symbol,
 string Name,
 decimal Condition,
 int Integrity,
 string Description,
 int ModuleSlots,
 int MountingPoints,
 int FuelCapacity,
 Requirements Requirements,
 int Quality
    );

    public record Module(
 string Symbol,
 string Name,
 string Description,
 int Capacity,
 int Range,
 Requirements Requirements
    );

    public record Mount(
 string Symbol,
 string Name,
 string Description,
 int Strength,
 IReadOnlyList<string> Deposits,
 Requirements Requirements
    );

    public record Reactor(
 string Symbol,
 string Name,
 decimal Condition,
 int Integrity,
 string Description,
 int PowerOutput,
 Requirements Requirements,
 int Quality
    );

    public record Requirements(
 int Power,
 int Crew,
 int? Slots
    );

public record Shipyard(
    string Symbol,
    IReadOnlyList<ShipType> ShipTypes,
    IReadOnlyList<Transaction> Transactions,
    [property:JsonPropertyName("ships")]
    IReadOnlyList<ShipFrame> ShipFrames,
    int ModificationsFee
);

    public record ShipFrame(
 string Type,
 string Name,
 string Description,
 string Activity,
 string Supply,
 int PurchasePrice,
 Frame Frame,
 Reactor Reactor,
 Engine Engine,
 IReadOnlyList<Module> Modules,
 IReadOnlyList<Mount> Mounts,
 Crew Crew,
 string WaypointSymbol
    );

    public record ShipType(
 string Type,
 string Waypoint
    );

    public record Transaction(
 string WaypointSymbol,
 string ShipType,
 int Price,
 string AgentSymbol,
 DateTime Timestamp
    );

