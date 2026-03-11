using System.Diagnostics.Contracts;
using System.Text.Json;
using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Models.Results;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class FulfillContractCommand(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    ITransactionsCacheService _transactionsService,
    IContractsService _contractsService,
    IShipLogsService _shipLogsService,
    IShipsService _shipsService
) : IShipCommandsService
{
    private const int COUNT_BEFORE_LOOP = 20;
    private const int LOOP_WAIT_IN_MINUTES = 10;

    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        STContract? contract = null;
        if (ship.Goal is not null)
        {
            contract = await _contractsService.GetAsync(ship.Goal);
            if (contract.Fulfilled)
            {
                ship = ship with { Goal = null };
                contract = null;
            }
        }
        if (contract is null)
        {
            contract = await _contractsService.GetActiveAsync();
            if (contract is null)
            {
                contract = await _contractsService.GetActiveAsync(refresh: true);
            }

            if (contract is null)
            {
                if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
                {
                    var dockResult = await _shipsService.DockAsync(ship.Symbol);
                    ship = ship with { Nav = dockResult };
                }
                var negotiateResult = await _contractsService.NegotiateAsync(ship.Symbol);
                var acceptResult = await _contractsService.AcceptAsync(negotiateResult.Contract.Id);
                contract = STContractApi.MapToSTContract(acceptResult.Contract);
            }
            if (contract is not null)
            {
                ship = ship with { Goal = contract.ContractId };
            }
        }
        var goal = ship.Goal;
        var count = 0;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if ((currentWaypoint.Marketplace is not null && currentWaypoint.Marketplace.TradeGoods is null)
            || (currentWaypoint.Shipyard is not null && currentWaypoint.Shipyard.ShipFrames is null))
        {
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        }

        while (true)
        {
            count++;
            if (count > COUNT_BEFORE_LOOP)
            {
                var timespan = TimeSpan.FromMinutes(LOOP_WAIT_IN_MINUTES);
                ship = ship with { Cooldown = new Cooldown(ship.Symbol, (int)timespan.TotalSeconds, (int)timespan.TotalSeconds, DateTime.UtcNow.AddMinutes(LOOP_WAIT_IN_MINUTES)) };
                return new ShipStatus(ship, $"Stuck in a loop.", DateTime.UtcNow);
            }
            var cooldownDelay = ShipsService.GetShipCooldown(ship);
            if (cooldownDelay is not null) return shipStatus;

            //await Task.Delay(500);

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                await _transactionsService.SetAsync(refuelResponse.Transaction);
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForBuyOrFulfill(
                ship, 
                currentWaypoint, 
                contract?.Terms.Deliver[0].DestinationSymbol,
                contract?.Terms.Deliver[0].TradeSymbol);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
                continue;
            }

            if (contract is null)
            {
                if (ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
                {
                    contract = (await _contractsService.GetAsync()).SingleOrDefault(c => !c.Fulfilled && !c.Accepted);
                    if (contract is null)
                    {
                        var contractNegotiateResult = await _contractsService.NegotiateAsync(ship.Symbol);
                        contract = STContractApi.MapToSTContract(contractNegotiateResult.Contract);
                    }
                    await _contractsService.AcceptAsync(contract.ContractId);
                    continue;
                }
            }

            (STContract? newContract, Cargo? cargo, Agent? agent) = await _shipCommandsHelperService.FulfillContract(ship, contract);
            if (contract is not null && cargo is not null && agent is not null)
            {
                await _agentsService.SetAsync(agent);
                await AddFulfillShipLog(ship.Symbol, contract);
                contract = newContract;
                ship = ship with { Cargo = cargo, Goal = contract.ContractId };
                if (ship.Registration.Role == ShipRegistrationRolesEnum.COMMAND.ToString())
                {
                    ship = ship with { ShipCommand = null };
                    return new ShipStatus(ship, "Completed contract, reseting.", DateTime.UtcNow);
                }
                continue;
            }

            (nav, var fuel, var cooldown) = await _shipCommandsHelperService.NavigateToFulfillContract(ship, currentWaypoint, contract.Terms.Deliver[0].DestinationSymbol);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
                return new ShipStatus(ship, $"Navigate To FulfillContract {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
            }

            (nav, fuel, cooldown, bool noWork, goal) = await _shipCommandsHelperService.NavigateToMarketplaceExportForContract(
                ship, 
                currentWaypoint,
                contract.Terms.Deliver[0].TradeSymbol);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown, Goal = goal};
                return new ShipStatus(ship, $"Navigate To Marketplace Export for Contract {nav.Route.Destination.Symbol}", DateTime.UtcNow);
            }

            var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargoForContract(ship, currentWaypoint, contract.Terms.Deliver[0].TradeSymbol, contract.Terms.Deliver[0].UnitsRequired);
            if (purchaseCargoResult is not null)
            {
                ship = ship with { Cargo = purchaseCargoResult.Cargo };
                await _agentsService.SetAsync(purchaseCargoResult.Agent);
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            throw new SpaceTraderResultException("Infinite loop, no work planned.  BuyAndSellCommand", new HttpRequestException("Fake"), $"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
        }
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