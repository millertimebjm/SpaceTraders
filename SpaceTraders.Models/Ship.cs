namespace SpaceTraders.Models;

    public record Cargo(
 int Capacity,
 int Units,
 IReadOnlyList<Inventory> Inventory
    );

    public record Consumed(
 int Amount,
 DateTime Timestamp
    );

    public record Cooldown(
 string ShipSymbol,
 int TotalSeconds,
 int RemainingSeconds,
 DateTime Expiration
    );

//     public record Crew(
//  int Current,
//  int Required,
//  int Capacity,
//  string Rotation,
//  int Morale,
//  int Wages
//     );

    public record Destination(
 string Symbol,
 string Type,
 string SystemSymbol,
 int X,
 int Y
    );

//     public record Engine(
//  string Symbol,
//  string Name,
//  int Condition,
//  int Integrity,
//  string Description,
//  int Speed,
//  Requirements Requirements,
//  int Quality
//     );

//     public record Frame(
//  string Symbol,
//  string Name,
//  int Condition,
//  int Integrity,
//  string Description,
//  int ModuleSlots,
//  int MountingPoints,
//  int FuelCapacity,
//  Requirements Requirements,
//  int Quality
//     );

    public record Fuel(
 int Current,
 int Capacity,
 Consumed Consumed
    );

    public record Inventory(
 string Symbol,
 string Name,
 string Description,
 int Units
    );

//     public record Module(
//  string Symbol,
//  string Name,
//  string Description,
//  int Capacity,
//  int Range,
//  Requirements Requirements
//     );

//     public record Mount(
//  string Symbol,
//  string Name,
//  string Description,
//  int Strength,
//  IReadOnlyList<string> Deposits,
//  Requirements Requirements
//     );

    public record Nav(
 string SystemSymbol,
 string WaypointSymbol,
 Route Route,
 string Status,
 string FlightMode
    );

    public record Origin(
 string Symbol,
 string Type,
 string SystemSymbol,
 int X,
 int Y
    );

//     public record Reactor(
//  string Symbol,
//  string Name,
//  int Condition,
//  int Integrity,
//  string Description,
//  int PowerOutput,
//  Requirements Requirements,
//  int Quality
//     );

    public record Registration(
 string Name,
 string FactionSymbol,
 string Role
    );

    public record Ship(
 string Symbol,
 Registration Registration,
 Nav Nav,
 Crew Crew,
 Frame Frame,
 Reactor Reactor,
 Engine Engine,
 IReadOnlyList<Module> Modules,
 IReadOnlyList<Mount> Mounts,
 Cargo Cargo,
 Fuel Fuel,
 Cooldown Cooldown
    );

    public record Route(
 Destination Destination,
 Origin Origin,
 DateTime DepartureTime,
 DateTime Arrival
    );

