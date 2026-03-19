using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class ScrapShipCommand(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    ITransactionsCacheService _transactionsService,
    IShipsService _shipsService,
    IShipStatusesCacheService _shipStatusesCacheService
) : IShipCommandsService
{
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var cooldownDelay = ShipsService.GetShipCooldown(ship);
        if (cooldownDelay is not null) return shipStatus;
        
        Nav? nav;
        Fuel? fuel;
        Cooldown cooldown = ship.Cooldown;
        var goalModel = ship.GoalModel;

        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if ((currentWaypoint.Marketplace is not null && currentWaypoint.Marketplace.TradeGoods is null)
            || (currentWaypoint.Shipyard is not null && currentWaypoint.Shipyard.ShipFrames is null))
        {
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        }

        if (currentWaypoint.Shipyard is not null)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            var scrapShipResponse = await _shipsService.ScrapShipAsync(ship.Symbol);
            await _agentsService.SetAsync(scrapShipResponse.Agent);
            await _transactionsService.SetAsync(scrapShipResponse.Transaction);

            var ships = await _shipsService.GetAsync();
            var shipStatuses = (await _shipStatusesCacheService.GetAsync()).ToList();
            shipStatuses = shipStatuses.Where(ss => ships.Select(s => s.Symbol).Contains(ss.Ship.Symbol)).ToList();
            await _shipStatusesCacheService.SetAsync(shipStatuses);
            return null;
        }

        if (ship.Fuel.Current < ship.Fuel.Capacity
            && currentWaypoint.Marketplace?.Exchange.Any(e => e.Symbol == TradeSymbolsEnum.FUEL.ToString()) == true)
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

        if (goalModel is null)
        {
            var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
            var waypoints = system.Waypoints.ToList();
            var pathModelsWithBurn = PathsService.BuildSystemPathWithCostWithBurn(waypoints, ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
            
            var shipyardWaypointSymbols = waypoints.Where(w => w.Shipyard is not null).Select(w => w.Symbol).ToList();
            var shipyardWaypointPathModels = pathModelsWithBurn.Where(pm => shipyardWaypointSymbols.Contains(pm.WaypointSymbol));
            goalModel = new GoalModel(null, null, shipyardWaypointPathModels.First().WaypointSymbol);
        }

        if (goalModel?.SellWaypointSymbol is not null)
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Scrap {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);

            // if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            // {
            //     nav = await _shipsService.DockAsync(ship.Symbol);
            //     ship = ship with { Nav = nav };
            // }
            // var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(
            //     ship, 
            //     currentWaypoint, 
            //     goalModel.TradeSymbol, 
            //     contract.Terms.Deliver[0].UnitsRequired - contract.Terms.Deliver[0].UnitsFulfilled);
            // await _agentsService.SetAsync(purchaseCargoResult.Agent);
            // await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
            // ship = ship with { Cargo = purchaseCargoResult.Cargo };

            // nav = await _shipsService.OrbitAsync(ship.Symbol);
            // ship = ship with { Nav = nav };

            // (nav, fuel, cooldown) = await NavigateHelper(ship, goalModel.SellWaypointSymbol);
            // ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            // return new ShipStatus(ship, $"Navigate To Contract Fulfill {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }

        return null;
    }
}