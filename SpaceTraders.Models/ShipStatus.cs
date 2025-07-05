using SpaceTraders.Models.Enums;

namespace SpaceTraders.Models;

public record ShipStatus(
    Ship Ship,
    ShipCommandEnum? ShipCommandEnum,
    Cargo Cargo,
    string LastMessage,
    DateTime DateTimeOfLastInstruction
);