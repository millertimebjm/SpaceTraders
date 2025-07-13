using Microsoft.VisualBasic;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class BuyAndSellCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsService _waypointsService;
    private readonly ISystemsService _systemsService;
    private readonly IAgentsService _agentsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly IShipJobsFactory _shipJobsFactory;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.BuyToSell;
    public BuyAndSellCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IShipsService shipsService,
        IWaypointsService waypointsService,
        ISystemsService systemsService,
        IShipStatusesCacheService shipStatusesCacheService,
        IShipJobsFactory shipJobsFactory,
        IAgentsService agentsService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _shipsService = shipsService;
        _waypointsService = waypointsService;
        _systemsService = systemsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _shipJobsFactory = shipJobsFactory;
        _agentsService = agentsService;
    }

    public async Task<Ship> Run(
        Ship ship,
        Dictionary<string, Ship> shipsDictionary)
    {
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return ship;
            var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
            var inventorySymbols = ship.Cargo.Inventory.Select(i => i.Symbol).ToHashSet();

            var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

            var sellingWaypoint = paths.Select(p => p.Key)
                .Where(w => w.Marketplace is not null
                    && w.Marketplace.Imports.Count(i => inventorySymbols.Contains(i.Symbol)) > 0)
                .OrderByDescending(w =>
                    w.Marketplace?.Imports.Count(i => inventorySymbols.Contains(i.Symbol)))
                .FirstOrDefault();

            await Task.Delay(2000);

            var fuel = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (fuel is not null)
            {
                ship = ship with { Fuel = fuel };
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForBuyAndSell(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
                continue;
            }

            var cargo = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
            if (cargo is not null)
            {
                ship = ship with { Cargo = cargo };
                if (cargo.Units == 0 && ship.Registration.Role == ShipRegistrationRolesEnum.COMMAND.ToString())
                {
                    ship = ship with { ShipCommand = null };
                    await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, ship.ShipCommand?.ShipCommandEnum, ship.Cargo, $"Resetting Job.", DateTime.UtcNow));
                    return ship;
                }
                continue;
            }

            var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(ship, currentWaypoint);
            if (purchaseCargoResult is not null)
            {
                ship = ship with { Cargo = purchaseCargoResult.Cargo };
                await _agentsService.SetAsync(purchaseCargoResult.Agent);
                continue;
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            (nav, fuel) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"NavigateToMarketplaceImport {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow));
                return ship;
            }

            (nav, fuel, var noWork) = await _shipCommandsHelperService.NavigateToMarketplaceRandomExport(ship, currentWaypoint);
            if (noWork)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"No Valid Exports found", DateTime.UtcNow));
                return ship;
            }
            else if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"NavigateToMarketplaceRandomExport {nav.Route.Destination.Symbol}", DateTime.UtcNow));
                return ship;
            }

            throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
        }
    }
}