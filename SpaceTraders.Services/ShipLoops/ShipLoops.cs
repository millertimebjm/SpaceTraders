using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipLoops;

public class ShipLoopsService(
    IShipStatusesCacheService _shipStatusesCacheService,
    IShipsService _shipsService,
    ISystemsService _systemsService,
    IShipJobsFactory _shipJobsFactory,
    IShipCommandsServiceFactory _shipCommandsServiceFactory,
    IWaypointsService _waypointsService,
    ILogger<ShipLoopsService> _logger,
    ITradesService _tradesService
) : IShipLoopsService
{
    public async Task Run()
    {
        var ships = await _shipsService.GetAsync();
        await _shipStatusesCacheService.DeleteAsync();
        foreach (var ship in ships)
        {
            var shipStatus = new ShipStatus(ship, "No instructions set.", DateTime.UtcNow);
            await _shipStatusesCacheService.SetAsync(shipStatus);
        }

        var systemSymbols = ships.Select(s => s.Nav.SystemSymbol).ToList();
        foreach (var systemSymbol in systemSymbols)
        {
            var systemCache = await _systemsService.GetAsync(systemSymbol);
            foreach (var waypoint in systemCache.Waypoints.Where(w => w.SystemSymbol is null).ToList())
            {
                _logger.LogInformation("Refreshing Waypoint {waypoint}", waypoint.Symbol);
                await _waypointsService.GetAsync(waypoint.Symbol, refresh: true);
                await Task.Delay(500);
            }
        }

        while (true)
        {
            var shipStatuses = (await _shipStatusesCacheService.GetAsync()).ToList();
            shipStatuses = shipStatuses.Where(ss => ss.Ship.Registration.Role != ShipRegistrationRolesEnum.SATELLITE.ToString()).ToList();
            for (int i = 0; i < shipStatuses.Count(); i++)
            {
                var ship = shipStatuses[i].Ship;
                var shipStatus = shipStatuses[i];
                if (ShipsService.GetShipCooldown(ship) is not null) continue;

                if (ship.ShipCommand is null)
                {
                    var shipJobsService = _shipJobsFactory.Get(Enum.Parse<ShipRegistrationRolesEnum>(shipStatus.Ship.Registration.Role));
                    if (shipJobsService is null)
                    {
                        ship = ship with { ShipCommand = null };
                        shipStatus = shipStatus with { Ship = ship };
                        await _shipStatusesCacheService.SetAsync(shipStatus);
                        continue;
                    }

                    var shipCommand = await shipJobsService.Get(shipStatuses.Select(ss => ss.Ship).ToList(), shipStatus.Ship);
                    ship = ship with { ShipCommand = shipCommand };
                    shipStatus = shipStatus with { Ship = ship };
                    await _shipStatusesCacheService.SetAsync(shipStatus);
                    shipStatuses[i] = shipStatus;
                    if (shipCommand is null) continue;
                }

                var shipCommandService = _shipCommandsServiceFactory.Get(ship.ShipCommand.ShipCommandEnum);

                try
                {
                    var shipStatusesDictionary = shipStatuses.ToDictionary(ss => ss.Ship.Symbol, ss => ss.Ship);
                    shipStatus = await shipCommandService.Run(
                        shipStatus,
                        shipStatusesDictionary);
                    ship = shipStatus.Ship;
                    ship = ship with { Error = null };
                    shipStatus = shipStatus with { Ship = ship };
                    shipStatuses[i] = shipStatus;
                }
                catch (SpaceTraderResultException ex)
                {
                    var timeSpan = TimeSpan.FromMinutes(10);
                    ship = ship with
                    {
                        Error = ex.Message + " " + ex.InnerException.Message + " " + ex.ResponseBody,
                        Cooldown = new Cooldown(ship.Symbol, (int)timeSpan.TotalSeconds, (int)timeSpan.TotalSeconds, DateTime.UtcNow.Add(timeSpan))
                    };
                    ship = ship with { ShipCommand = null };
                    shipStatus = shipStatus with { Ship = ship };
                    shipStatuses[i] = shipStatus;
                }
                await _shipStatusesCacheService.SetAsync(shipStatuses[i]);

                await Task.Delay(1000);
            }

            //var system = await _systemsService.GetAsync(shipStatuses.First().Ship.Nav.SystemSymbol);
            var systems = await _systemsService.GetAsync();
            var waypoints = systems.SelectMany(s => s.Waypoints).ToList();
            await _tradesService.SaveTradeModelsAsync(waypoints, 400, 400);

            TimeSpan? shortestCooldown = null;
            foreach (var shipStatus in shipStatuses)
            {
                var cooldown = ShipsService.GetShipCooldown(shipStatus.Ship);
                if (cooldown is null || cooldown.Value.TotalSeconds < 0)
                {
                    shortestCooldown = null;
                    break;
                }

                if (shortestCooldown is null || cooldown.Value < shortestCooldown.Value)
                    shortestCooldown = cooldown.Value;
            }
            if (shortestCooldown is not null
                && shortestCooldown.Value.TotalMilliseconds > 0)
            {
                await Task.Delay(shortestCooldown.Value);
            }
        }
    }
    
    public static DateTime? MinimumDate(DateTime? d1, DateTime? d2)
    {
        if (d1 is null) return d2;
        if (d2 is null) return d1;
        if (d1 < d2) return d1;
        return d2;
    }
}