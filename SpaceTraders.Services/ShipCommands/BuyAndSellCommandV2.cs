using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class BuyAndSellCommandV2(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    ITransactionsService _transactionsService
) : IShipCommandsService
{
    private const int COUNT_BEFORE_LOOP = 20;
    private const int LOOP_WAIT_IN_MINUTES = 10;

    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var count = 0;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        if ((currentWaypoint.Marketplace is not null && currentWaypoint.Marketplace.TradeGoods is null)
            || (currentWaypoint.Shipyard is not null && currentWaypoint.Shipyard.ShipFrames is null))
        {
            currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
        }

        Nav? nav;
        Fuel? fuel;
        Cooldown? cooldown;

        count++;
        if (count > COUNT_BEFORE_LOOP)
        {
            var timespan = TimeSpan.FromMinutes(LOOP_WAIT_IN_MINUTES);
            ship = ship with { Cooldown = new Cooldown(ship.Symbol, (int)timespan.TotalSeconds, (int)timespan.TotalSeconds, DateTime.UtcNow.AddMinutes(LOOP_WAIT_IN_MINUTES)) };
            return new ShipStatus(ship, $"Stuck in a loop.", DateTime.UtcNow);
        }
        var cooldownDelay = ShipsService.GetShipCooldown(ship);
        if (cooldownDelay is not null) return shipStatus;
        var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
        var inventorySymbols = ship.Cargo.Inventory.Select(i => i.Symbol).ToHashSet();

        await Task.Delay(1000);

        if (_shipCommandsHelperService.IsFuelNeeded(ship)
            && _shipCommandsHelperService.IsWaypointFuelAvailable(currentWaypoint))
        {
            if (ship.Nav.Status != NavStatusEnum.DOCKED.ToString())
            {
                nav = await _shipCommandsHelperService.Dock(ship, currentWaypoint);
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
            }
            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            ship = ship with { Fuel = refuelResponse.Fuel };
            await _agentsService.SetAsync(refuelResponse.Agent);
            await _transactionsService.SetAsync(refuelResponse.Transaction);
        }

        if (_shipCommandsHelperService.IsAnyItemToSellAtCurrentWaypoint(ship, currentWaypoint))
        {
            var sellCargoResponse = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
            if (sellCargoResponse is not null)
            {
                ship = ship with { Cargo = sellCargoResponse.Cargo };
                await _agentsService.SetAsync(sellCargoResponse.Agent);
                var firstHauler = shipsDictionary
                    .Where(s => s.Value.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString())
                    .OrderBy(s => s.Key)
                    .FirstOrDefault();
                if (sellCargoResponse.Cargo.Units == 0
                    && (ship.Registration.Role == ShipRegistrationRolesEnum.COMMAND.ToString()
                        || ship.Symbol == firstHauler.Key))
                {
                    ship = ship with { ShipCommand = null };
                    return new ShipStatus(ship, $"{shipStatus.LastMessage} - Resetting Job.", DateTime.UtcNow);
                }
            }
        }

        if (ship.Cargo.Units > 0)
        {
            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
                return new ShipStatus(ship, $"NavigateToMarketplaceImport {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
            }
        }

        var destinationWaypoint = await _shipCommandsHelperService.GetClosestSellingWaypoint(ship, currentWaypoint);
        if (destinationWaypoint.Symbol != currentWaypoint.Symbol)
        {
            await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            (nav, fuel, cooldown, var noWork) = await _shipCommandsHelperService.NavigateToMarketplaceRandomExport(ship, currentWaypoint);
            if (noWork)
            {
                return new ShipStatus(ship, $"No Valid Exports found", DateTime.UtcNow);
            }
            else if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown};
                return new ShipStatus(ship, $"NavigateToMarketplaceRandomExport {nav.Route.Destination.Symbol}", DateTime.UtcNow);
            }
        }
        
        var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(ship, currentWaypoint);
        if (purchaseCargoResult is not null)
        {
            ship = ship with { Cargo = purchaseCargoResult.Cargo };
            await _agentsService.SetAsync(purchaseCargoResult.Agent);

            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
                return new ShipStatus(ship, $"NavigateToMarketplaceImport {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow);
            }
        }

        throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
    }
}