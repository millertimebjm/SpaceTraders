using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Accounts.Interfaces;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.ServerStatusServices.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
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
    IServerStatusService _serverStatusService,
    IMongoCollectionFactory _collectionFactory,
    IAccountService _accountService,
    IConfiguration _configuration,
    IShipCommandsHelperService _shipCommandHelperService
) : IShipLoopsService
{
    public async Task Run()
    {
        if (!await _collectionFactory.DatabaseExists())
        {
            await _accountService.RegisterAsync();
        }
        
        var serverStatus = await _serverStatusService.GetAsync();
        if (serverStatus.ServerResets.Next.AddHours(-1) < DateTime.UtcNow)
        {
            _logger.LogInformation("Waiting for next reset.");
            await Task.Delay(DateTime.UtcNow - serverStatus.ServerResets.Next);
            await _collectionFactory.DeleteDatabaseAsync();
            await _accountService.RegisterAsync();
        }
        var account = await _accountService.GetAsync();
        _configuration[$"SpaceTrader:" + ConfigurationEnums.AgentToken.ToString()] = account.Token;
        
        var ships = await _shipsService.GetAsync();
        await _shipStatusesCacheService.DeleteAsync();
        foreach (var ship in ships)
        {
            var shipStatus = new ShipStatus(ship, "No instructions set.", DateTime.UtcNow);
            await _shipStatusesCacheService.SetAsync(shipStatus);
        }

        while (true)
        {
            await UpdateSystemWaypoints(ships);
            await BuyNewShipIfPossible(ships);

            var shipStatuses = (await _shipStatusesCacheService.GetAsync()).ToList();
            shipStatuses = shipStatuses
                .OrderBy(s => {
                    var parts = s.Ship.Symbol.Split('-');
                    return Convert.ToInt32(parts[1], 16); // Parse as hex
                })
                .ToList();
            for (int i = 0; i < shipStatuses.Count(); i++)
            {
                var ship = shipStatuses[i].Ship;
                var shipStatus = shipStatuses[i];
                if (ShipsService.GetShipCooldown(ship) is not null) continue;
                if (ship.Symbol == "SPATIAL19-E"
                    || ship.Symbol == "SPATIAL19-10")
                {
                    int j = 0;
                }

                if (ship.ShipCommand is null)
                {
                    var shipJobsService = _shipJobsFactory.Get(shipStatus.Ship);
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
                    var timeSpan = TimeSpan.FromMinutes(2);
                    ship = await _shipsService.GetAsync(ship.Symbol);
                    if (ship.Cooldown is null)
                    {
                        ship = ship with { Cooldown = new Cooldown(ship.Symbol, (int)timeSpan.TotalSeconds, (int)timeSpan.TotalSeconds, DateTime.UtcNow.Add(timeSpan)) };
                    }
                    ship = ship with
                    {
                        Error = ex.Message + " " + ex.InnerException.Message + " " + ex.ResponseBody,
                    };
                    shipStatus = shipStatus with { Ship = ship };
                    shipStatuses[i] = shipStatus;
                }
                await _shipStatusesCacheService.SetAsync(shipStatuses[i]);

                await Task.Delay(1000);
            }

            _logger.LogInformation("Time until server reset: {hours} Hours", Math.Round((serverStatus.ServerResets.Next - DateTime.UtcNow).TotalHours));
            await SleepUntilNextShipReady(shipStatuses);

            var now = DateTime.UtcNow;
            if (serverStatus.ServerResets.Next.AddHours(-1) < DateTime.UtcNow)
            {
                _logger.LogInformation("Waiting for next reset.");
                var waitInMilliseconds = (int)(serverStatus.ServerResets.Next - now).TotalMilliseconds;
                await Task.Delay(waitInMilliseconds);
            }
        }
    }

    private async Task BuyNewShipIfPossible(IEnumerable<Ship> ships)
    {
        var shipToBuy = await _shipCommandHelperService.ShipToBuy(ships);
        if (shipToBuy.Item1 is not null && shipToBuy.Item2 is not null)
        {
            await _shipCommandHelperService.CheckRemotePurchaseShip(ships, shipToBuy.Item1, shipToBuy.Item2.Value);
        }
    }

    private async Task SleepUntilNextShipReady(List<ShipStatus> shipStatuses)
    {
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
            _logger.LogInformation("Sleeping for {duration}", Math.Round(shortestCooldown.Value.TotalMinutes, 2));
            await Task.Delay(shortestCooldown.Value);
        }
    }

    public async Task UpdateSystemWaypoints(IEnumerable<Ship> ships)
    {
        var systemSymbols = ships
            .Select(s => s.Nav.SystemSymbol)
            .Distinct()
            .ToList();
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
    }

    public static DateTime? MinimumDate(DateTime? d1, DateTime? d2)
    {
        if (d1 is null) return d2;
        if (d2 is null) return d1;
        if (d1 < d2) return d1;
        return d2;
    }
}