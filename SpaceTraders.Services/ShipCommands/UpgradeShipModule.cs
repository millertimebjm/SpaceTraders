using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
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

public class UpgradeShipModule(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    ITransactionsCacheService _transactionsService,
    ITradesService _tradesService,
    IShipsService _shipsService
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
            goalModel = await _shipCommandsHelperService.GetShipModuleGoalModel(ship);
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

        if (ship.Cargo.Units < 1 && goalModel.BuyWaypointSymbol == ship.Nav.WaypointSymbol)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            if (ship.Cargo.Units < 1)
            {
                var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(ship, currentWaypoint, goalModel.TradeSymbol, 1);
                if (purchaseCargoResult is null)
                {
                    ship = ship with {GoalModel = null};
                    return new ShipStatus(ship, $"Resetting goal because of bad GoalModel", DateTime.UtcNow);
                }
                ship = ship with { Cargo = purchaseCargoResult.Cargo };
                await _agentsService.SetAsync(purchaseCargoResult.Agent);
                await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
            }
        }

        if (ship.Cargo.Units > 0 && currentWaypoint.Shipyard is not null)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            var moduleToRemove = _shipCommandsHelperService.GetModuleToRemove(ship, goalModel.TradeSymbol);
            if (moduleToRemove is not null)
            {
                var removeModuleResult = await _shipsService.RemoveModule(ship.Symbol, moduleToRemove);
                ship = ship with { Modules = removeModuleResult.Modules, Cargo = removeModuleResult.Cargo };
            }

            var installModuleResult = await _shipsService.InstallModule(ship.Symbol, goalModel.TradeSymbol);
            ship = ship with { Modules = installModuleResult.Modules, Cargo = installModuleResult.Cargo };

            var cargo = await _shipsService.JettisonAsync(ship.Symbol, moduleToRemove, 1);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown, Goal = null, GoalModel = await _shipCommandsHelperService.GetShipModuleGoalModel(ship), Cargo = cargo, ShipCommand = null };
            if (ship.GoalModel is null)
            {
                return new ShipStatus(ship, $"Install module complete, resetting...", DateTime.UtcNow);
            }
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

        if (ship.Cargo.Units > 0)
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            if (goalModel.SellWaypointSymbol is null)
            {
                goalModel = await _shipCommandsHelperService.GetShipModuleGoalModel(ship);
            }
            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Shipyard {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
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
