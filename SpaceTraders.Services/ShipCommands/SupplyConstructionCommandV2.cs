using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class SupplyConstructionCommandV2(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    IWaypointsCacheService _waypointsCacheService,
    ITransactionsCacheService _transactionsService,
    IShipsService _shipsService,
    ITradesService _tradesService
) : IShipCommandsService
{
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if (currentWaypoint.Marketplace is not null)
        {
            currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
        }
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var constructionWaypoint = system.Waypoints.FirstOrDefault(w => w.JumpGate is not null && w.IsUnderConstruction);
        if (constructionWaypoint is null) 
        {
            ship = ship with { ShipCommand = null, GoalModel = null };
            return new ShipStatus(ship, "No construction waypoint to contribute.", DateTime.UtcNow);
        }

        GoalModel? goalModel = ship.GoalModel;
        if (goalModel is null)
        {
            goalModel = await _shipCommandsHelperService.BuildSupplyConstructionGoalModel(ship, constructionWaypoint);
            ship = ship with { GoalModel = goalModel };
        }
        if (goalModel is null)
        {
            ship = ship with { ShipCommand = null, GoalModel = null };
            return new ShipStatus(ship, "No Goal Model could be created.", DateTime.UtcNow);
        }

        Nav? nav = ship.Nav;
        Fuel? fuel = ship.Fuel;
        Cooldown cooldown = ship.Cooldown;

        if (ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.TradeGoods.Any(tg => tg.Symbol == TradeSymbolsEnum.FUEL.ToString()) == true)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is null) throw new SpaceTraderResultException("Refuel failed.");
            ship = ship with { Fuel = refuelResponse.Fuel };
            await _agentsService.SetAsync(refuelResponse.Agent);
            await _transactionsService.SetAsync(refuelResponse.Transaction);
        }

        if (ship.Cargo.Units == 0 && ship.Nav.WaypointSymbol != goalModel.BuyWaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.BuyWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigating to Buying waypoint.", DateTime.UtcNow);
        }

        if (ship.Cargo.Units == 0 && ship.Nav.WaypointSymbol == goalModel.BuyWaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            var purchaseCargoResult = await _shipCommandsHelperService.BuyForConstruction(ship, currentWaypoint, constructionWaypoint);
            if (purchaseCargoResult is null) 
            {
                ship = ship with { GoalModel = null, ShipCommand = null };
                return new ShipStatus(ship, "Construction material buy didn't work.", DateTime.UtcNow);
            }
            ship = ship with { Cargo = purchaseCargoResult.Cargo };
            await _agentsService.SetAsync(purchaseCargoResult.Agent);
            await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
        }

        if (ship.Cargo.Units > 0 && ship.Nav.WaypointSymbol != goalModel.SellWaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigating to Construction waypoint.", DateTime.UtcNow);
        }

        if (ship.Cargo.Units > 0 && ship.Nav.WaypointSymbol == goalModel.SellWaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            var supplyResult = await _shipCommandsHelperService.SupplyConstructionSite(ship, currentWaypoint);
            ship = ship with { Cargo = supplyResult.Cargo };
            currentWaypoint = currentWaypoint with { Construction = supplyResult.Construction };
            await _waypointsCacheService.SetAsync(currentWaypoint);
            ship = ship with { ShipCommand = null, GoalModel = null };
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
            return new ShipStatus(ship, $"Construction supplied, resetting job.", DateTime.UtcNow);
        }

        throw new SpaceTraderResultException("Infinite loop, no work planned. SupplyConstruction", new HttpRequestException("Fake"), $"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
    }
}