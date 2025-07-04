using SpaceTraders.Models.Enums;

namespace SpaceTraders.Models;

public record ShipStatus(
    string Symbol,
    ShipCommandEnum? ShipCommandEnum,
    Cargo Cargo,
    string LastMessage
);