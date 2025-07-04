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

    public record Destination(
 string Symbol,
 string Type,
 string SystemSymbol,
 int X,
 int Y
    );

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

