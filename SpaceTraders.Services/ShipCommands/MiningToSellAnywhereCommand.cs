using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class MiningToSellAnywhereCommand : IShipCommandsService
{
    private readonly IShipCommandsHelperService _shipCommandsHelperService;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsService _waypointsService;
    private readonly ISystemsService _systemsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly IShipJobsFactory _shipJobsFactory;
    private readonly ShipCommandEnum _shipCommandEnum = ShipCommandEnum.MiningToSellAnywhere;
    public MiningToSellAnywhereCommand(
        IShipCommandsHelperService shipCommandsHelperService,
        IShipsService shipsService,
        IWaypointsService waypointsService,
        ISystemsService systemsService,
        IShipStatusesCacheService shipStatusesCacheService,
        IShipJobsFactory shipJobsFactory)
    {
        _shipCommandsHelperService = shipCommandsHelperService;
        _shipsService = shipsService;
        _waypointsService = waypointsService;
        _systemsService = systemsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _shipJobsFactory = shipJobsFactory;
    }

    public async Task<Ship> Run(
        Ship ship,
        Dictionary<string, Ship> shipsDictionary)
    {
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        var miningWaypoint = await _waypointsService.GetAsync(ship.ShipCommand.StartWaypointSymbol);
        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return ship;
            var sellingWaypoint = await _shipCommandsHelperService.GetClosestSellingWaypoint(ship, currentWaypoint);

            await Task.Delay(2000);

            var executed = await _shipCommandsHelperService.Jettison(ship);
            if (executed) continue;

            var fuel = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (fuel is not null)
            {
                ship = ship with { Fuel = fuel };
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForMiningToSellAnywhere(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);

                continue;
            }

            var cargo = await _shipCommandsHelperService.Sell(ship, currentWaypoint);
            if (cargo is not null)
            {
                ship = ship with { Cargo = cargo };
                if (cargo.Units == 0)
                {
                    var shipJobsService = _shipJobsFactory.Get(ship);
                    if (shipJobsService is null)
                    {
                        ship = ship with { ShipCommand = null };
                        return ship;
                    }
                    ship = ship with { ShipCommand = await shipJobsService.Get(shipsDictionary.Select(sd => sd.Value), ship) };
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

            (nav, fuel) = await _shipCommandsHelperService.NavigateToStartWaypoint(ship, currentWaypoint, miningWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"NavigateToStartWaypoint {ship.ShipCommand.StartWaypointSymbol}", DateTime.UtcNow));
                return ship;
            }

            (cargo, Cooldown? cooldown) = await _shipCommandsHelperService.Extract(ship, currentWaypoint, miningWaypoint);
            if (cargo is not null && cooldown is not null)
            {
                ship = ship with { Cargo = cargo, Cooldown = cooldown };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"Extract {miningWaypoint.Symbol}", DateTime.UtcNow));
                return ship;
            }

            (nav, fuel) = await _shipCommandsHelperService.NavigateToMarketplaceImport(ship, currentWaypoint, sellingWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, _shipCommandEnum, ship.Cargo, $"NavigateToMarketplaceImport {sellingWaypoint?.Symbol}", DateTime.UtcNow));
                return ship;
            }

            throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");
        }
    }
}