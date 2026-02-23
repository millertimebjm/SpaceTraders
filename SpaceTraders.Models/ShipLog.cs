using SpaceTraders.Models.Enums;

namespace SpaceTraders.Models;

public record ShipLog(
    string ShipSymbol,
    ShipLogEnum ShipLogEnum,
    string JsonData,
    DateTime StartedDateTimeUtc,
    DateTime CompletedDateTimeUtc
);