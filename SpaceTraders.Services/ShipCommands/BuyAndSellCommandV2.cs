using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
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
            goalModel = await GetGoalModelAsync(ship);
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

            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint);
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
            (nav, fuel, cooldown) = await NavigateHelper(ship, goalModel.BuyWaypointSymbol);
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
            (nav, fuel, cooldown) = await NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Marketplace Export {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }

        return null;
            
        //     var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        //     var inventorySymbols = ship.Cargo.Inventory.Select(i => i.Symbol).ToHashSet();

        //     var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
        //     if (refuelResponse is not null)
        //     {
        //         ship = ship with { Fuel = refuelResponse.Fuel };
        //         await _agentsService.SetAsync(refuelResponse.Agent);
        //         await _transactionsService.SetAsync(refuelResponse.Transaction);
        //         continue;
        //     }

        //     GoalModel? goalModel = ship.GoalModel;

        //     var (nav, goal) = await _shipCommandsHelperService.DockForBuyAndSell(ship, currentWaypoint);
        //     if (nav is not null)
        //     {
        //         ship = ship with { Nav = nav, Goal = goal };
        //         currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        //         continue;
        //     }

        //     var sellCargoResponse = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
        //     if (sellCargoResponse is not null)
        //     {
        //         ship = ship with { Cargo = sellCargoResponse.Cargo, ShipCommand = null, GoalModel = null };
        //         await _agentsService.SetAsync(sellCargoResponse.Agent);
        //         return new ShipStatus(ship, $"Completed BuyToSell, Resetting Job.", DateTime.UtcNow);
        //     }

        //     (nav, var fuel, var cooldown) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint);
        //     if (nav is not null && fuel is not null)
        //     {
        //         ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
        //         return new ShipStatus(ship, $"Navigate To Marketplace Import {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        //     }

        //     var otherShipGoalSymbols = shipsDictionary
        //         .Values
        //         .Where(s => s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.BuyToSell && s.GoalModel is not null)
        //         .Select(s => s.GoalModel?.TradeSymbol ?? "")
        //         .ToList();
        //     (nav, fuel, cooldown, var noWork, goalModel) = await _shipCommandsHelperService.NavigateToMarketplaceRandomExport(
        //         ship, 
        //         currentWaypoint,
        //         otherShipGoalSymbols);
        //     if (noWork)
        //     {
        //         var timeSpan = TimeSpan.FromMinutes(10);
        //         ship = ship with {
        //             Goal = null,
        //             GoalModel = null,
        //             ShipCommand = new ShipCommand(ship.Symbol, ShipCommandEnum.HaulingAssistToSellAnywhere)
        //         };
        //         return new ShipStatus(ship, $"No Valid Exports found", DateTime.UtcNow);
        //     }
        //     else if (nav is not null && fuel is not null)
        //     {
        //         ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown, GoalModel = goalModel};
        //         return new ShipStatus(ship, $"Navigate To Marketplace Random Export {nav.Route.Destination.Symbol}", DateTime.UtcNow);
        //     }

        //     var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(ship, currentWaypoint);
        //     if (purchaseCargoResult is not null)
        //     {
        //         ship = ship with { Cargo = purchaseCargoResult.Cargo, Goal = null };
        //         await _agentsService.SetAsync(purchaseCargoResult.Agent);
        //     }

        //     nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
        //     if (nav is not null)
        //     {
        //         ship = ship with { Nav = nav };
        //         continue;
        //     }

        //     throw new SpaceTraderResultException("Infinite loop, no work planned.  BuyAndSellCommand", new HttpRequestException("Fake"), $"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
        // }
    }

    private async Task<(Nav?, Fuel?, Cooldown?)> NavigateHelper(Ship ship, string waypointSymbol)
    {
        var paths = await _pathsService.BuildSystemPathWithCost(ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
        var path = paths.Single(p => p.WaypointSymbol == waypointSymbol);
        var nextHop = path.PathWaypointSymbols[1];

        Nav? nav = null;
        Fuel? fuel = null;
        Cooldown cooldown = ship.Cooldown;

        if (WaypointsService.ExtractSystemFromWaypoint(nextHop) != WaypointsService.ExtractSystemFromWaypoint(waypointSymbol))
        {
            (nav, cooldown) = await _shipsService.JumpAsync(nextHop, ship.Symbol);
            return (nav, fuel, cooldown);
        }
        (nav, fuel) = await _shipsService.NavigateAsync(nextHop, ship);
        return (nav, fuel, cooldown);
    }

    private async Task<GoalModel?> GetGoalModelAsync(Ship ship)
    {
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(ship.Nav.WaypointSymbol));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        if (ship.Cargo.Units > 0)
        {
            //BuildSellModel(IReadOnlyList<Waypoint> waypoints, string originWaypoint = null, int? fuelMax = 0, int? fuelCurrent = 0)
            var sellModels = _tradesService.BuildSellModel(waypoints, ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
            var inventory = ship.Cargo.Inventory.OrderByDescending(i => i.Units).FirstOrDefault();
            var validSellModels = sellModels.Where(sm => sm.TradeSymbol == inventory.Symbol).ToList();
            var bestSellModel = _tradesService.GetBestSellModel(validSellModels);
            return new GoalModel(bestSellModel.TradeSymbol, null, bestSellModel.WaypointSymbol);
        }
        else
        {
            var tradeModels = await _tradesService.GetTradeModelsAsync(waypoints, ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
            var bestTrade = _tradesService.GetBestOrderedTradesWithTravelCost(tradeModels).FirstOrDefault();
            if (bestTrade is null) return null;
            return new GoalModel(bestTrade.TradeSymbol, bestTrade.ExportWaypointSymbol, bestTrade.ImportWaypointSymbol);
        }
    }
}
