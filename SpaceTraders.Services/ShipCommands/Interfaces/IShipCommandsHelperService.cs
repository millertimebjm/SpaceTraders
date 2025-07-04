using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsHelperService
{
    Task<Fuel?> Refuel(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> Orbit(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> Dock(Ship ship, Waypoint currentWaypoint);
    Task<DateTime?> NavigateToEndWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint);
    Task<DateTime?> NavigateToStartWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint startWaypoint);
    Task<Cargo?> Sell(Ship ship, Waypoint currentWaypoint);
    Task<Cargo?> Buy(Ship ship, Waypoint currentWaypoint);
    Task<SupplyResult?> SupplyConstructionSite(Ship ship, Waypoint currentWaypoint);
    Task<DateTime?> Extract(Ship ship, Waypoint currentWaypoint, Waypoint miningWaypoint);
    Task<bool> Jettison(Ship ship);
    Task<DateTime?> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint);
    Task<DateTime?> NavigateToConstructionWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<Waypoint?> GetClosestSellingWaypoint(Ship ship, Waypoint currentWaypoint);
    Task<DateTime?> NavigateToMarketplaceExport(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<Cargo?> BuyForConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
}