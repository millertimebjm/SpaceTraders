using SpaceTraders.Models.Enums;

namespace SpaceTraders.Models;

public record ShipStatus(
    Ship Ship,
    string LastMessage,
    DateTime DateTimeOfLastInstruction
);