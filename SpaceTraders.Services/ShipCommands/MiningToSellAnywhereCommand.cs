using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class MiningToSellAnywhereCommand(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    IAgentsService _agentsService,
    ITransactionsService _transactionsService
) : IShipCommandsService
{
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return shipStatus;
            var sellingWaypoint = await _shipCommandsHelperService.GetClosestSellingWaypoint(ship, currentWaypoint);

            await Task.Delay(1000);

            var executed = await _shipCommandsHelperService.Jettison(ship);
            if (executed) continue;

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                await _transactionsService.SetAsync(refuelResponse.Transaction);
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForMiningToSellAnywhere(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
                continue;
            }

            (nav, var fuel) = await _shipCommandsHelperService.NavigateToMiningWaypoint(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Error = null };
                return new ShipStatus(ship, $"NavigateToStartWaypoint {nav.WaypointSymbol}", DateTime.Now);
            }

            (var cargo, Cooldown? cooldown) = await _shipCommandsHelperService.Extract(ship, currentWaypoint);
            if (cargo is not null && cooldown is not null)
            {
                ship = ship with { Cargo = cargo, Cooldown = cooldown, Error = null  };
                return new ShipStatus(ship, $"Extract {ship.Nav.WaypointSymbol}", DateTime.UtcNow);
            }

            (nav, fuel, cooldown) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
                return new ShipStatus(ship, $"NavigateToMarketplaceImport {nav.Route.Destination.Symbol}", DateTime.UtcNow);
            }

            var sellCargoResponse = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
            if (sellCargoResponse is not null)
            {
                ship = ship with { Cargo = sellCargoResponse.Cargo };
                await _agentsService.SetAsync(sellCargoResponse.Agent);
                if (sellCargoResponse.Cargo.Units == 0 && ship.Registration.Role == ShipRegistrationRolesEnum.COMMAND.ToString())
                {
                    ship = ship with { ShipCommand = null, Error = null  };
                    return new ShipStatus(ship, $"Resetting Job.", DateTime.UtcNow);
                }
                continue;
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
        }
    }
}