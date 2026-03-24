using SpaceTraders.Models;
using SpaceTraders.Models.Enums;

namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsHelperService
{
    Task<RefuelResponse?> Refuel(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> Orbit(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForFuel(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForShipyard(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForMiningToSellAnywhere(Ship ship, Waypoint currentWaypoint);
    Task<Nav?> DockForSupplyConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<(Nav?, Fuel?)> NavigateToEndWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint endWaypoint);
    Task<(Nav?, Fuel?)> NavigateToStartWaypoint(Ship ship, Waypoint currentWaypoint, Waypoint startWaypoint);
    Task<SellCargoResponse?> Sell(Ship ship, Waypoint currentWaypoint);
    Task<PurchaseCargoResult?> PurchaseCargo(Ship ship, Waypoint currentWaypoint, string tradeSymbol, int maxQuantity = int.MaxValue);
    Task<SupplyResult?> SupplyConstructionSite(Ship ship, Waypoint currentWaypoint);
    Task<(Cargo?, Cooldown?)> Extract(Ship ship, Waypoint currentWaypoint);
    Task<(Cargo?, Cooldown?)> Siphon(Ship ship, Waypoint currentWaypoint);
    Task<Cargo?> Jettison(Ship ship);
    Task<(Nav?, Fuel?, Cooldown)> NavigateToMarketplaceImport(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToConstructionWaypoint(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToMarketplaceExport(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<PurchaseCargoResult?> BuyForConstruction(Ship ship, Waypoint currentWaypoint, Waypoint constructionWaypoint);
    Task<(Nav?, Fuel?)> NavigateToSurvey(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?)> NavigateToMiningWaypoint(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?, Cooldown)> NavigateToSiphonWaypoint(Ship ship, Waypoint currentWaypoint);

    Task<Cooldown> Survey(Ship ship);
    Task<(Nav? nav, Fuel? fuel, Cooldown cooldown)> NavigateToShipyard(Ship ship, Waypoint currentWaypoint);
    Task<PurchaseShipResponse?> PurchaseShip(Ship ship, Waypoint currentWaypoint);
    bool IsFuelNeeded(Ship ship);
    bool IsWaypointFuelAvailable(Waypoint waypoint);
    Task<Nav?> Dock(Ship ship, Waypoint waypoint);
    bool IsAnyItemToSellAtCurrentWaypoint(Ship ship, Waypoint waypoint);
    Task<Nav?> DockForBuyOrFulfill(
        Ship ship, 
        Waypoint currentWaypoint, 
        string contractWaypointSymbol, 
        string inventorySymbol);
    Task<PurchaseCargoResult?> PurchaseCargoForContract(Ship ship, Waypoint currentWaypoint, string contractTradeSymbol, int amountToBuy);
    Task<(Nav?, Fuel?, Cooldown?)> NavigateToFulfillContract(Ship ship, Waypoint currentWaypoint, string contractDestinationWaypointSymbol);
    Task<(STContract?, Cargo?, Agent?)> FulfillContract(Ship ship, STContract contract);
    Task<(string?, ShipTypesEnum?)> ShipToBuy(IEnumerable<Ship> ships);
    Task<bool> CheckRemotePurchaseShip(IEnumerable<Ship> ships, string shipyardWaypoint, ShipTypesEnum shipType);
    Task<Cargo?> TransferCargo(Ship ship, Waypoint currentWaypoint);
    Task<(Nav?, Fuel?, Cooldown)> NavigateHelper(Ship ship, string waypointSymbol);
}