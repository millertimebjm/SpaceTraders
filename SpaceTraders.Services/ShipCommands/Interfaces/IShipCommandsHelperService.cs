using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsHelperService
{
    Task<bool> Refuel(Ship ship, Waypoint currentWaypoint);
    Task<bool> Orbit(Ship ship, Waypoint currentWaypoint);
    Task<bool> Dock(Ship ship, Waypoint currentWaypoint);
    Task<DateTime?> NavigateToEndWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint);
    Task<DateTime?> NavigateToStartWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint startWaypoint);
    Task<bool> Sell(Ship ship, Waypoint currentWaypoint);
    Task<bool> Buy(Ship ship, Waypoint currentWaypoint);
    Task<bool> SupplyConstructionSite(Ship ship, Waypoint currentWaypoint);
    Task<DateTime?> Extract(Ship ship, Waypoint currentWaypoint, Waypoint miningWaypoint);
    Task<bool> Jettison(Ship ship);
    Task<DateTime?> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint);
    Task<Waypoint?> GetClosestSellingWaypoint(Ship ship, Waypoint currentWaypoint);
}