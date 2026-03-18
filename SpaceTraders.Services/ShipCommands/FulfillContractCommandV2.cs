using System.Diagnostics.Contracts;
using System.Text.Json;
using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Models.Results;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class FulfillContractCommandV2(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    ITransactionsCacheService _transactionsService,
    IContractsService _contractsService,
    IShipLogsService _shipLogsService,
    IShipsService _shipsService,
    ITradesService _tradesService,
    IPathsService _pathsService
) : IShipCommandsService
{
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var cooldownDelay = ShipsService.GetShipCooldown(ship);
        if (cooldownDelay is not null) return shipStatus;

        STContract? contract = null;
        var goalModel = ship.GoalModel;
        
        Nav? nav;
        Fuel? fuel;
        Cooldown cooldown = ship.Cooldown;
        Cargo? cargo;

        if (goalModel is null)
        {
            contract = await GetLatestUnfulfilledContract();
            if (contract is null || !contract.Accepted)
            {
                if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
                {
                    nav = await _shipsService.DockAsync(ship.Symbol);
                    ship = ship with { Nav = nav };
                }
                if (contract is null)
                {
                    var contractNegotiateResult = await _contractsService.NegotiateAsync(ship.Symbol);
                    contract = STContractApi.MapToSTContract(contractNegotiateResult.Contract);
                }
                var contractAcceptResult = await _contractsService.AcceptAsync(ship.Symbol);
                await _agentsService.SetAsync(contractAcceptResult.Agent);
                contract = STContractApi.MapToSTContract(contractAcceptResult.Contract);
            }
            ArgumentNullException.ThrowIfNull(contract, nameof(contract));
            goalModel = await SetGoalModelByContract(contract, ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
            ArgumentNullException.ThrowIfNull(goalModel, nameof(goalModel));
            ship = ship with { GoalModel = goalModel };
        }

        if (contract is null)
        {
            contract = await _contractsService.GetActiveAsync();
        }

        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if ((currentWaypoint.Marketplace is not null && currentWaypoint.Marketplace.TradeGoods is null)
            || (currentWaypoint.Shipyard is not null && currentWaypoint.Shipyard.ShipFrames is null))
        {
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
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

        if (currentWaypoint.Symbol == goalModel.SellWaypointSymbol
            && ship.Cargo.Units > 0)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }

            var contractDeliverResult = await _contractsService.DeliverAsync(contract.ContractId, ship.Symbol, ship.Cargo.Inventory[0].Symbol, ship.Cargo.Inventory[0].Units);
            contract = STContractApi.MapToSTContract(contractDeliverResult.Contract);
            ship = ship with { Cargo = contractDeliverResult.Cargo };
            if (contract.Terms.Deliver[0].UnitsFulfilled == contract.Terms.Deliver[0].UnitsRequired)
            {
                var contractFulfillResult = await _contractsService.FulfillAsync(contract.ContractId);
                contract = STContractApi.MapToSTContract(contractFulfillResult.Contract);
                await _agentsService.SetAsync(contractFulfillResult.Agent);

                var contractNegotiateResult = await _contractsService.NegotiateAsync(ship.Symbol);
                contract = STContractApi.MapToSTContract(contractNegotiateResult.Contract);

                var contractAcceptResult = await _contractsService.AcceptAsync(contract.ContractId);
                contract = STContractApi.MapToSTContract(contractAcceptResult.Contract);
                await _agentsService.SetAsync(contractAcceptResult.Agent);

                goalModel = await SetGoalModelByContract(contract, ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current);
                ArgumentNullException.ThrowIfNull(goalModel, nameof(goalModel));
                ship = ship with { GoalModel = goalModel };
            }
        }

        if (currentWaypoint.Symbol == goalModel.BuyWaypointSymbol 
            && ship.Cargo.Units == 0)
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipsService.DockAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(
                ship, 
                currentWaypoint, 
                goalModel.TradeSymbol, 
                contract.Terms.Deliver[0].UnitsRequired - contract.Terms.Deliver[0].UnitsFulfilled);
            await _agentsService.SetAsync(purchaseCargoResult.Agent);
            await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
            ship = ship with { Cargo = purchaseCargoResult.Cargo };

            nav = await _shipsService.OrbitAsync(ship.Symbol);
            ship = ship with { Nav = nav };

            (nav, fuel, cooldown) = await NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Contract Fulfill {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
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

        if (ship.Cargo.Units > 0)
        {
            if (ship.Nav.Status != NavStatusEnum.IN_ORBIT.ToString())
            {
                nav = await _shipsService.OrbitAsync(ship.Symbol);
                ship = ship with { Nav = nav };
            }
            (nav, fuel, cooldown) = await NavigateHelper(ship, goalModel.SellWaypointSymbol);
            ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
            return new ShipStatus(ship, $"Navigate To Contract Fulfill {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
        }

        return null;
    }

    private async Task<GoalModel> SetGoalModelByContract(STContract contract, string originWaypoint, int fuelMax, int fuelCurrent)
    {
        var tradeSymbol = contract.Terms.Deliver[0].TradeSymbol;
        var tradeModels = await _tradesService.GetTradeModelsAsync(originWaypoint, fuelMax, fuelCurrent);
        var tradeModelsOnTradeSymbol = tradeModels.Where(tm => tm.TradeSymbol == tradeSymbol).ToList();
        var pathModels = await _pathsService.BuildSystemPathWithCost(originWaypoint, fuelMax, fuelCurrent);
        var pathModelsToTradeSymbol = pathModels
            .Where(pm => tradeModelsOnTradeSymbol
            .Select(tm => tm.ExportWaypointSymbol)
            .Contains(pm.WaypointSymbol))
            .OrderBy(pm => pm.TimeCost)
            .ThenBy(pm => pm.WaypointSymbol) // tiebreaker
            .First();

        return new GoalModel(tradeSymbol, pathModelsToTradeSymbol.WaypointSymbol, contract.Terms.Deliver[0].DestinationSymbol);
    }

    private async Task<(Nav?, Fuel?, Cooldown?)> NavigateHelper(Ship ship, string waypointSymbol)
    {
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints, ship.Nav.WaypointSymbol, ship.Fuel.Capacity, ship.Fuel.Current, waypointSymbol);
        var path = paths.Single(p => p.WaypointSymbol == waypointSymbol);
        var nextHop = path.PathWaypoints[1];

        Nav? nav = null;
        Fuel? fuel = null;
        Cooldown cooldown = ship.Cooldown;

        if (WaypointsService.ExtractSystemFromWaypoint(nextHop.WaypointSymbol) != WaypointsService.ExtractSystemFromWaypoint(ship.Nav.WaypointSymbol))
        {
            (nav, cooldown) = await _shipsService.JumpAsync(nextHop.WaypointSymbol, ship.Symbol);
            return (nav, fuel, cooldown);
        }
        nav = await _shipsService.NavToggleAsync(ship, nextHop.FlightModeEnum);
        (nav, fuel) = await _shipsService.NavigateAsync(nextHop.WaypointSymbol, ship);
        return (nav, fuel, cooldown);
    }

    private async Task<STContract?> GetLatestUnfulfilledContract()
    {
        var contract = await _contractsService.GetActiveAsync();
        if (contract is not null) return contract;

        var contracts = await _contractsService.GetAsync();
        contract = contracts.OrderByDescending(c => c.DeadlineToAccept).FirstOrDefault();
        if (contract is null || contract.Fulfilled)
        {
            contracts = await _contractsService.GetAsync(refresh: true);
            contract = contracts.OrderByDescending(c => c.DeadlineToAccept).FirstOrDefault();
        }

        if (contract is null || contract.Fulfilled)
        {
            return null;
        }
        
        return contract;
    }

    private async Task AddFulfillShipLog(string shipSymbol, STContract contract)
    {
        var datetime = DateTime.UtcNow;
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.FulfillContract,
            JsonSerializer.Serialize(new
            {
                ContractId = contract.ContractId,
                InventorySymbol = contract.Terms.Deliver[0].TradeSymbol,
                InventoryUnits = contract.Terms.Deliver[0].UnitsRequired,
                TotalCredits = contract.Terms.Payment.OnAccepted + contract.Terms.Payment.OnFulfilled,
            }),
            datetime,
            datetime
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    
}