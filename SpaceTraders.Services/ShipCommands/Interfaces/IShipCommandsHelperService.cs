using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsHelperService
{
    Task<RefuelResponse?> Refuel(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> Orbit(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForFuel(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForShipyard(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForMiningToSellAnywhere(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForBuyAndSell(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForSupplyConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<(Nav?, Fuel?)> NavigateToEndWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint);
    Task<(Nav?, Fuel?)> NavigateToStartWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint startWaypoint);
    Task<SellCargoResponse?> Sell(Ship ship, Waypoint currentWaypoint);
    Task<PurchaseCargoResult?> PurchaseCargo(Ship ship, Waypoint currentWaypoint);
    Task<SupplyResult?> SupplyConstructionSite(Ship ship, Waypoint currentWaypoint);
    Task<(Cargo?, Cooldown?)> Extract(Ship ship, Waypoint currentWaypoint);
    Task<bool> Jettison(Ship ship);
    Task<(Nav?, Fuel?, Cooldown?)> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToConstructionWaypoint(Ship ship, Waypoint currentWaypoint);
    Task<Waypoint?> GetClosestSellingWaypoint(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToMarketplaceExport(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<PurchaseCargoResult?> BuyForConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<(Nav? nav, Fuel? fuel, Cooldown? cooldown, bool noWork)> NavigateToMarketplaceRandomExport(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToSurvey(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToMiningWaypoint(Ship ship, Waypoint currentWaypoint);

    Task<Cooldown> Survey(Ship ship);
    Task<(Nav? nav, Fuel? fuel)> NavigateToShipyard(Ship ship, Waypoint currentWaypoint);
    Task<PurchaseShipResponse> PurchaseShip(Ship ship, Waypoint currentWaypoint);
    Task<(Nav nav, Fuel fuel, Cooldown cooldown)> NavigateToExplore(Ship ship, Waypoint currentWaypoint);
    Task<PurchaseCargoResult?> PurchaseFuelForRescue(Ship ship, Waypoint currentWaypoint, int v);
    Task<(Nav nav, Fuel fuel)> NavigateToShipToRescue(Ship ship, Waypoint currentWaypoint, Waypoint rescueShipWaypoint);
}