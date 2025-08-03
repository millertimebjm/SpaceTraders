using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class RescueFuelCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsService _waypointsService;
    private readonly ISystemsService _systemsService;
    private readonly IAgentsService _agentsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly ITransactionsService _transactionsService;
    private readonly IMarketplacesService _marketplacesService;
    public RescueFuelCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IShipsService shipsService,
        IWaypointsService waypointsService,
        ISystemsService systemsService,
        IShipStatusesCacheService shipStatusesCacheService,
        IAgentsService agentsService,
        ITransactionsService transactionsService,
        IMarketplacesService marketplacesService)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _shipsService = shipsService;
        _waypointsService = waypointsService;
        _systemsService = systemsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _agentsService = agentsService;
        _transactionsService = transactionsService;
        _marketplacesService = marketplacesService;
    }

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
            var system = await _systemsService.GetAsync(currentWaypoint.SystemSymbol);
            var inventorySymbols = ship.Cargo.Inventory.Select(i => i.Symbol).ToHashSet();

            // var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

            await Task.Delay(1000);

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                await _transactionsService.SetAsync(refuelResponse.Transaction);
                continue;
            }

            var purchaseCargoResult = await _shipCommandsHelperService.PurchaseFuelForRescue(ship, currentWaypoint, 40);
            if (purchaseCargoResult is not null)
            {
                await _agentsService.SetAsync(purchaseCargoResult.Agent);
                await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForFuel(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
                continue;
            }

            Waypoint destinationWaypoint = null;
            (nav, Fuel fuel) = await _shipCommandsHelperService.NavigateToShipToRescue(ship, currentWaypoint, destinationWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                return new ShipStatus(ship, $"NavigateToMarketplaceRandomExport {nav.Route.Destination.Symbol}", DateTime.UtcNow);
            }

            (nav, fuel, var cooldown, var noWork) = await _shipCommandsHelperService.NavigateToMarketplaceRandomExport(ship, currentWaypoint);
            if (noWork)
            {
                return new ShipStatus(ship, $"No Valid Exports found", DateTime.UtcNow);
            }
            else if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel, Cooldown = cooldown };
                return new ShipStatus(ship, $"NavigateToMarketplaceRandomExport {nav.Route.Destination.Symbol}", DateTime.UtcNow);
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