using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships;
using SpaceTraders.Services.Ships.Interfaces;
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
        if (ship.Fuel is null || ship.Cargo is null)
        {
            var apiShip = await _shipsService.GetAsync(ship.Symbol);
            ship = ship with { Fuel = apiShip.Fuel, Cargo = apiShip.Cargo};
        }
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
            var system = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(currentWaypoint.Symbol));
            var jumpGateWaypoint = system.Waypoints.SingleOrDefault(w => w.JumpGate is not null && !w.IsUnderConstruction);
            List<string> otherShipGoalModelTradeSymbols = [];
            if (jumpGateWaypoint is null)
            {
                otherShipGoalModelTradeSymbols = shipsDictionary
                    .Values
                    .Where(s => 
                        s.GoalModel?.TradeSymbol is not null 
                        && s.Symbol != ship.Symbol
                        && s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.BuyToSell)
                    .Select(s => s.GoalModel!.TradeSymbol!)
                    .ToList();
            }
            
            var otherShipSystems = shipsDictionary
                .Values
                .Where(s => s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.BuyToSell
                    && s.Symbol != ship.Symbol)
                .Select(s => WaypointsService.ExtractSystemFromWaypoint(s.Nav.WaypointSymbol))
                .GroupBy(s => s)
                .Where(s => s.Count() > 4)
                .Select(s => s.Key);
            goalModel = await GetGoalModelAsync(ship, currentWaypoint, otherShipGoalModelTradeSymbols, otherShipSystems.ToList());
            ship = ship with { GoalModel = goalModel };
        }
        if (goalModel is null)
        {
            ship = ship with { ShipCommand = new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration), GoalModel = null, Goal = null };
            return new ShipStatus(ship, $"Nothing to do buy and sell, so exploring...", DateTime.UtcNow); 
        }
        
        Nav? nav = null;
        Fuel? fuel = null;
        Cooldown cooldown = ship.Cooldown;

        if (ship.Cargo.Units == 0 
            && goalModel.BuyWaypointSymbol == ship.Nav.WaypointSymbol 
            && goalModel.TradeSymbol is not null
            && goalModel.SellWaypointSymbol is not null)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(ship, currentWaypoint, goalModel.TradeSymbol);
            if (purchaseCargoResult is null)
            {
                ship = ship with {GoalModel = null};
                return new ShipStatus(ship, $"Resetting goal because of bad GoalModel", DateTime.UtcNow);
            }
            await _agentsService.SetAsync(purchaseCargoResult.Agent);
            await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
            ship = ship with { Cargo = purchaseCargoResult.Cargo };

            nav = await _shipsService.OrbitAsync(ship.Symbol);
            ship = ship with { Nav = nav };

            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav ?? ship.Nav, Fuel = fuel ?? ship.Fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Marketplace Import {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }

        if (ship.Cargo.Units > 0 && goalModel.SellWaypointSymbol == ship.Nav.WaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            if (ship.Fuel.Current < ship.Fuel.Capacity
                && currentWaypoint.Marketplace?.TradeGoods?.Any(tg => tg.Symbol == TradeSymbolsEnum.FUEL.ToString()) == true)
            {
                var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
                if (refuelResponse is null) throw new SpaceTraderResultException("Refuel failed");
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                await _transactionsService.SetAsync(refuelResponse.Transaction);
            }

            var sellCargoResult = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
            if (sellCargoResult is null)
            {
                ship = ship with { GoalModel = null };
                return new ShipStatus(ship, $"Reset Goal Model", DateTime.UtcNow); 
            }
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
            if (refuelResponse is null) throw new SpaceTraderResultException("Refuel failed.");
            ship = ship with { Fuel = refuelResponse.Fuel };
            await _agentsService.SetAsync(refuelResponse.Agent);
            await _transactionsService.SetAsync(refuelResponse.Transaction);
        }

        if (ship.Cargo.Units == 0 && goalModel.BuyWaypointSymbol is not null)
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.BuyWaypointSymbol);
            ship = ship with { Nav = nav ?? ship.Nav, Fuel = fuel ?? ship.Fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Marketplace Export {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }
        else if (goalModel.SellWaypointSymbol is not null)
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel ?? ship.Fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Marketplace Export {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }

        return null!;
    }

    private async Task<GoalModel?> GetGoalModelAsync(Ship ship, Waypoint currentWaypoint, List<string> otherShipGoalModelTradeSymbols, List<string> otherShipSystemsToAvoid)
    {
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(ship.Nav.WaypointSymbol));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();

        var originWaypoint = ship.Nav.WaypointSymbol;
        var originSystem = ship.Nav.SystemSymbol;
        if (otherShipSystemsToAvoid.Contains(originSystem))
        {
            var jumpGateWaypoints = waypoints.Where(w => w.JumpGate is not null && !otherShipSystemsToAvoid.Contains(WaypointsService.ExtractSystemFromWaypoint(w.Symbol)));
            var paths = await _pathsService.BuildSystemPathWithCostWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), originWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
            var pathsToClosestAvailableJumpGate = paths
                .Where(p => jumpGateWaypoints.Any(w => w.Symbol == p.WaypointSymbol))
                .OrderBy(p => p.TimeCost);
            originWaypoint = pathsToClosestAvailableJumpGate.First().WaypointSymbol;
        }

        if (ship.Cargo.Units > 0)
        {
            var sellModels = await _tradesService.GetSellModelsAsyncWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), currentWaypoint.Symbol, ship.Fuel.Capacity, ship.Fuel.Current);
            var inventory = ship.Cargo.Inventory.OrderByDescending(i => i.Units).First();
            var validSellModels = sellModels.Where(sm => sm.TradeSymbol == inventory.Symbol).ToList();
            var bestSellModel = validSellModels.OrderByDescending(sm => sm.NavigationFactor).FirstOrDefault();
            if (bestSellModel is null) return null;
            return new GoalModel(bestSellModel.TradeSymbol, null, bestSellModel.WaypointSymbol);
        }
        else
        {
            var tradeModels = await _tradesService.GetTradeModelsAsyncWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), originWaypoint, ship.Fuel.Capacity, ship.Fuel.Current, 3);
            tradeModels = tradeModels.Where(tm => !otherShipGoalModelTradeSymbols.Contains(tm.TradeSymbol)).ToList();
            var bestTradeModel = tradeModels.OrderByDescending(sm => sm.NavigationFactor).FirstOrDefault();
            if (bestTradeModel is not null)
            {
                return new GoalModel(bestTradeModel.TradeSymbol, bestTradeModel.ExportWaypointSymbol, bestTradeModel.ImportWaypointSymbol);
            }

            tradeModels = await _tradesService.GetTradeModelsAsyncWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current, 3);
            tradeModels = tradeModels.Where(tm => !otherShipGoalModelTradeSymbols.Contains(tm.TradeSymbol)).ToList();
            bestTradeModel = tradeModels.OrderByDescending(sm => sm.NavigationFactor).FirstOrDefault();
            if (bestTradeModel is null) return null;
            return new GoalModel(bestTradeModel.TradeSymbol, bestTradeModel.ExportWaypointSymbol, bestTradeModel.ImportWaypointSymbol);
        }
    }
}
