using SpaceTraders.Models.Enums;

namespace SpaceTraders.Models;

public record ShipCommand(
    string ShipSymbol,
    ShipCommandEnum ShipCommandEnum,
    string? StartWaypointSymbol,
    string? EndWaypointSymbol
);