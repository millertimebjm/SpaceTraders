namespace SpaceTraders.Mvc.Models;

public record ShipRevenueEvent(string ShipSymbol, int Amount, DateTime DateTimeUtc, TimeSpan Duration);