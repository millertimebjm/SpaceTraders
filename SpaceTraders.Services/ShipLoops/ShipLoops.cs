using System.ComponentModel;
using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipLoops;

public class ShipLoopsService : IShipLoopsService
{
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly IAgentsService _agentsService;
    private readonly IShipsService _shipsService;
    private readonly ISystemsService _systemsService;
    private readonly IShipJobsFactory _shipJobsFactory;
    private readonly IShipCommandsServiceFactory _shipCommandsServiceFactory;
    private readonly IWaypointsService _waypointsService;
    private readonly ILogger<ShipLoopsService> _logger;

    public ShipLoopsService(
        IShipStatusesCacheService shipStatusesCacheService,
        IAgentsService agentsService,
        IShipsService shipsService,
        ISystemsService systemsService,
        IShipJobsFactory shipJobsFactory,
        IShipCommandsServiceFactory shipCommandsServiceFactory,
        IWaypointsService waypointsService,
        ILogger<ShipLoopsService> logger
    )
    {
        _shipStatusesCacheService = shipStatusesCacheService;
        _agentsService = agentsService;
        _shipsService = shipsService;
        _systemsService = systemsService;
        _shipJobsFactory = shipJobsFactory;
        _shipCommandsServiceFactory = shipCommandsServiceFactory;
        _waypointsService = waypointsService;
        _logger = logger;
    }

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
                for (int i = 0; i < shipStatuses.Count(); i++)
                {
                    var ship = shipStatuses[i].Ship;

                    if (ship.ShipCommand is null)
                    {
                        var shipJobsService = _shipJobsFactory.Get(Enum.Parse<ShipRegistrationRolesEnum>(shipStatuses[i].Ship.Registration.Role));
                        if (shipJobsService is null)
                        {
                            ship = ship with { ShipCommand = null };
                            shipStatuses[i] = shipStatuses[i] with { Ship = ship };
                            await _shipStatusesCacheService.SetAsync(shipStatuses[i]);
                            continue;
                        }

                        var shipCommand = await shipJobsService.Get(shipStatuses.Select(ss => ss.Ship).ToList(), shipStatuses[i].Ship);
                        ship = ship with { ShipCommand = shipCommand };
                        shipStatuses[i] = shipStatuses[i] with { Ship = ship };
                        await _shipStatusesCacheService.SetAsync(shipStatuses[i]);
                        if (shipCommand is null) continue;
                    }

                    var shipCommandService = _shipCommandsServiceFactory.Get(ship.ShipCommand.ShipCommandEnum);

                    try
                    {
                        ship = await shipCommandService.Run(
                            ship,
                            shipStatuses.ToDictionary(ss => ss.Ship.Symbol, ss => ss.Ship));
                        ship = ship with { Error = null };
                    }
                    catch (Exception ex)
                    {
                        var timeSpan = TimeSpan.FromMinutes(10);
                        ship = ship with
                        {
                            Error = ex.Message,
                            Cooldown = new Cooldown(ship.Symbol, (int)timeSpan.TotalSeconds, (int)timeSpan.TotalSeconds, DateTime.UtcNow.Add(timeSpan))
                        };
                        shipStatuses[i] = shipStatuses[i] with { Ship = ship };
                        await _shipStatusesCacheService.SetAsync(shipStatuses[i]);
                    }

                    await Task.Delay(1000);
                }

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