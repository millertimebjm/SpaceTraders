using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsHelperService
{
    Task<Fuel?> Refuel(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> Orbit(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForFuel(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForShipyard(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForMiningToSellAnywhere(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForBuyAndSell(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForSupplyConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<(Nav?, Fuel?)> NavigateToEndWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint);
    Task<(Nav?, Fuel?)> NavigateToStartWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint startWaypoint);
    Task<Cargo?> Sell(Ship ship, Waypoint currentWaypoint);
    Task<Cargo?> Buy(Ship ship, Waypoint currentWaypoint);
    Task<SupplyResult?> SupplyConstructionSite(Ship ship, Waypoint currentWaypoint);
    Task<(Cargo?, Cooldown?)> Extract(Ship ship, Waypoint currentWaypoint);
    Task<bool> Jettison(Ship ship);
    Task<(Nav?, Fuel?)> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToConstructionWaypoint(Ship ship, Waypoint currentWaypoint);
    Task<Waypoint?> GetClosestSellingWaypoint(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToMarketplaceExport(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<Cargo?> BuyForConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<(Nav nav, Fuel fuel)> NavigateToMarketplaceRandomExport(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToSurvey(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToMiningWaypoint(Ship ship, Waypoint currentWaypoint);

    Task<Cooldown> Survey(Ship ship);
    Task<(Nav? nav, Fuel? fuel)> NavigateToShipyard(Ship ship, Waypoint currentWaypoint);
    Task<bool> PurchaseShip(Ship ship, Waypoint currentWaypoint);
}