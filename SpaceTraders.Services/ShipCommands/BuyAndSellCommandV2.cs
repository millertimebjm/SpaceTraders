using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class BuyAndSellCommandV2(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    ITransactionsCacheService _transactionsService,
    ITradesService _tradesService,
    IShipsService _shipsService,
    IPathsService _pathsService
) : IShipCommandsService
{
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if ((currentWaypoint.Marketplace is not null && currentWaypoint.Marketplace.TradeGoods is null)
            || (currentWaypoint.Shipyard is not null && currentWaypoint.Shipyard.ShipFrames is null))
        {
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        }

        var cooldownDelay = ShipsService.GetShipCooldown(ship);
        if (cooldownDelay is not null) return shipStatus;

        var goalModel = ship.GoalModel;
        if (goalModel is null)
        {
            var otherShipGoalModelTradeSymbols = shipsDictionary
                .Values
                .Where(s => 
                    s.GoalModel?.TradeSymbol is not null 
                    && s.Symbol != ship.Symbol
                    && s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.BuyToSell)
                .Select(s => s.GoalModel!.TradeSymbol)
                .ToList();
            goalModel = await GetGoalModelAsync(ship, currentWaypoint, otherShipGoalModelTradeSymbols);
            ship = ship with { GoalModel = goalModel };
        }
        if (goalModel is null)
        {
            ship = ship with { ShipCommand = null, GoalModel = null, Goal = null };
            return new ShipStatus(ship, $"Nothing to do buy and sell.", DateTime.UtcNow); 
        }
        
        Nav? nav = null;
        Fuel? fuel = null;
        Cooldown cooldown = ship.Cooldown;
        Agent? agent = null;

        if (ship.Cargo.Units == 0 && goalModel.BuyWaypointSymbol == ship.Nav.WaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(ship, currentWaypoint, goalModel.TradeSymbol);
            await _agentsService.SetAsync(purchaseCargoResult.Agent);
            await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
            ship = ship with { Cargo = purchaseCargoResult.Cargo };

            nav = await _shipsService.OrbitAsync(ship.Symbol);
            ship = ship with { Nav = nav };

            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Marketplace Import {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }

        if (ship.Cargo.Units > 0 && goalModel.SellWaypointSymbol == ship.Nav.WaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            if (ship.Fuel.Current < ship.Fuel.Capacity)
            {
                var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                await _transactionsService.SetAsync(refuelResponse.Transaction);
            }

            var sellCargoResult = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
            await _agentsService.SetAsync(sellCargoResult.Agent);
            await _transactionsService.SetAsync(sellCargoResult.Transaction);
            ship = ship with { Cargo = sellCargoResult.Cargo };

            nav = await _shipsService.OrbitAsync(ship.Symbol);
            ship = ship with { Nav = nav };

            ship = ship with { ShipCommand = null, GoalModel = null, Goal = null };
            return new ShipStatus(ship, $"Resetting job after Buy and Sell.", DateTime.UtcNow); 
        }

        if (ship.Fuel is null)
        {
            var updatedShip = await _shipsService.GetAsync(ship.Symbol);
            ship = ship with { Fuel = updatedShip.Fuel };
        }

        if (ship.Fuel.Current < ship.Fuel.Capacity && currentWaypoint.Marketplace?.Exchange.Any(e => e.Symbol == TradeSymbolsEnum.FUEL.ToString()) == true)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            ship = ship with { Fuel = refuelResponse.Fuel };
            await _agentsService.SetAsync(refuelResponse.Agent);
            await _transactionsService.SetAsync(refuelResponse.Transaction);
        }

        if (ship.Cargo.Units == 0)
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.BuyWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Marketplace Export {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }
        else
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Marketplace Export {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }

        return null;
    }

    private async Task<GoalModel?> GetGoalModelAsync(Ship ship, Waypoint currentWaypoint, List<string> otherShipGoalModelTradeSymbols)
    {
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(ship.Nav.WaypointSymbol));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        if (ship.Cargo.Units > 0)
        {
            var sellModels = await _tradesService.GetSellModelsAsyncWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
            var inventory = ship.Cargo.Inventory.OrderByDescending(i => i.Units).FirstOrDefault();
            var validSellModels = sellModels.Where(sm => sm.TradeSymbol == inventory.Symbol).ToList();
            var bestSellModel = validSellModels.OrderByDescending(sm => sm.NavigationFactor).FirstOrDefault();
            return new GoalModel(bestSellModel.TradeSymbol, null, bestSellModel.WaypointSymbol);
        }
        else
        {
            var tradeModels = await _tradesService.GetTradeModelsAsyncWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
            tradeModels = tradeModels.Where(tm => !otherShipGoalModelTradeSymbols.Contains(tm.TradeSymbol)).ToList();
            var bestTradeModel = tradeModels.OrderByDescending(sm => sm.NavigationFactor).FirstOrDefault();
            if (bestTradeModel is null) return null;
            return new GoalModel(bestTradeModel.TradeSymbol, bestTradeModel.ExportWaypointSymbol, bestTradeModel.ImportWaypointSymbol);
        }
    }
}
