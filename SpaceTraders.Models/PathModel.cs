namespace SpaceTraders.Models;

public record PathModel(string WaypointSymbol, List<string> PathWaypointSymbols, int TimeCost, int ResultFuel);