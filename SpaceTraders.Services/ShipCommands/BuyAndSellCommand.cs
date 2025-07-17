using Microsoft.VisualBasic;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
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
    private readonly IPathsService _pathsService;
    private readonly ITransactionsService _transactionsService;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.BuyToSell;
    public BuyAndSellCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IShipsService shipsService,
        IWaypointsService waypointsService,
        ISystemsService systemsService,
        IShipStatusesCacheService shipStatusesCacheService,
        IShipJobsFactory shipJobsFactory,
        IAgentsService agentsService,
        IPathsService pathsService,
        ITransactionsService transactionsService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _shipsService = shipsService;
        _waypointsService = waypointsService;
        _systemsService = systemsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _shipJobsFactory = shipJobsFactory;
        _agentsService = agentsService;
        _pathsService = pathsService;
        _transactionsService = transactionsService;
    }

    private const int COUNT_BEFORE_LOOP = 20;
    private const int LOOP_WAIT_IN_MINUTES = 10;

    public async Task<Ship> Run(
        Ship ship,
        Dictionary<string, Ship> shipsDictionary)
    {
        var count = 0;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);

        while (true)
        {
            count++;
            if (count > COUNT_BEFORE_LOOP)
            {
                var timespan = TimeSpan.FromMinutes(LOOP_WAIT_IN_MINUTES);
                ship = ship with { Cooldown = new Cooldown(ship.Symbol, (int)timespan.TotalSeconds, (int)timespan.TotalSeconds, DateTime.UtcNow.AddMinutes(LOOP_WAIT_IN_MINUTES)) };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"Stuck in a loop.", DateTime.UtcNow));
                return ship;
            }
            var cooldown = ShipsService.GetShipCooldown(ship);
            if (cooldown is not null) return ship;
            var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
            var inventorySymbols = ship.Cargo.Inventory.Select(i => i.Symbol).ToHashSet();

            var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

            var sellingWaypoint = paths.Select(p => p.Key)
                .Where(w => w.Marketplace is not null
                    && w.Marketplace.Imports.Count(i => inventorySymbols.Contains(i.Symbol)) > 0)
                .OrderByDescending(w =>
                    w.Marketplace?.Imports.Count(i => inventorySymbols.Contains(i.Symbol)))
                .FirstOrDefault();

            await Task.Delay(2000);

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                await _transactionsService.SetAsync(refuelResponse.Transaction);
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForBuyAndSell(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
                continue;
            }

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
                    await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"Resetting Job.", DateTime.UtcNow));
                    await _transactionsService.SetAsync(sellCargoResponse.Transaction);
                    return ship;
                }
                continue;
            }

            var purchaseCargoResult = await _shipCommandsHelperService.PurchaseCargo(ship, currentWaypoint);
            if (purchaseCargoResult is not null)
            {
                ship = ship with { Cargo = purchaseCargoResult.Cargo };
                await _agentsService.SetAsync(purchaseCargoResult.Agent);
                await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
                continue;
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            (nav, var fuel) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"NavigateToMarketplaceImport {ship.Nav.Route.Destination.Symbol}", DateTime.UtcNow));
                return ship;
            }

            (nav, fuel, var noWork) = await _shipCommandsHelperService.NavigateToMarketplaceRandomExport(ship, currentWaypoint);
            if (noWork)
            {
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"No Valid Exports found", DateTime.UtcNow));
                return ship;
            }
            else if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, $"NavigateToMarketplaceRandomExport {nav.Route.Destination.Symbol}", DateTime.UtcNow));
                return ship;
            }

            throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
        }
    }
}