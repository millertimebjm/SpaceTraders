using SpaceTraders.Models.Enums;

namespace SpaceTraders.Models;

public record PathModelWithBurn(string WaypointSymbol, List<PathWaypointWithBurn> PathWaypoints, int TimeCost, int ResultFuel);
public record PathWaypointWithBurn(string WaypointSymbol, NavFlightModeEnum FlightModeEnum);