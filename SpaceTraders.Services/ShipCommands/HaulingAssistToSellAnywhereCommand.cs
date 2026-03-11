using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class HaulingAssistToSellAnywhereCommand(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    IAgentsService _agentsService,
    ITransactionsCacheService _transactionsService
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

            if (ship.Cargo.Capacity == ship.Cargo.Units
                && (currentWaypoint.Type.Contains(WaypointTypesEnum.ENGINEERED_ASTEROID.ToString())
                || currentWaypoint.Type.Contains(WaypointTypesEnum.ENGINEERED_ASTEROID.ToString())))
            {
                ship = ship with { Cooldown = new Cooldown(ship.Symbol, 60, 60, DateTime.UtcNow.AddSeconds(60)) };
                shipStatus = shipStatus with { Ship = ship, DateTimeOfLastInstruction = DateTime.UtcNow, LastMessage = "Waiting for full cargo." };
                return shipStatus;
            }

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
                return new ShipStatus(ship, $"Navigate To Start Waypoint {nav.WaypointSymbol}", DateTime.UtcNow);
            }

            (nav, fuel, var cooldown) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
                return new ShipStatus(ship, $"Navigate To Marketplace Import {nav.Route.Destination.Symbol}", DateTime.UtcNow);
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