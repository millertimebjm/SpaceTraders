using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class MiningToSellAnywhereCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IWaypointsService _waypointsService;
    private readonly IAgentsService _agentsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly ITransactionsService _transactionsService;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.MiningToSellAnywhere;
    public MiningToSellAnywhereCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IWaypointsService waypointsService,
        IShipStatusesCacheService shipStatusesCacheService,
        IAgentsService agentsService,
        ITransactionsService transactionsService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _waypointsService = waypointsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _agentsService = agentsService;
        _transactionsService = transactionsService;
    }

    public async Task<Ship> Run(
        Ship ship,
        Dictionary<string, Ship> shipsDictionary)
    {
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return ship;
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
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"NavigateToStartWaypoint {nav.WaypointSymbol}", DateTime.UtcNow));
                return ship;
            }

            (var cargo, Cooldown? cooldown) = await _shipCommandsHelperService.Extract(ship, currentWaypoint);
            if (cargo is not null && cooldown is not null)
            {
                ship = ship with { Cargo = cargo, Cooldown = cooldown };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"Extract {ship.Nav.WaypointSymbol}", DateTime.UtcNow));
                return ship;
            }

            (nav, fuel) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"NavigateToMarketplaceImport {nav.Route.Destination.Symbol}", DateTime.UtcNow));
                return ship;
            }

            var sellCargoResponse = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
            if (sellCargoResponse is not null)
            {
                ship = ship with { Cargo = sellCargoResponse.Cargo };
                await _agentsService.SetAsync(sellCargoResponse.Agent);
                if (sellCargoResponse.Cargo.Units == 0 && ship.Registration.Role == ShipRegistrationRolesEnum.COMMAND.ToString())
                {
                    ship = ship with { ShipCommand = null };
                    await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"Resetting Job.", DateTime.UtcNow));
                    return ship;
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