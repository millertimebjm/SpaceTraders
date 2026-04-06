using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Accounts.Interfaces;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ServerStatusServices.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.Ships;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Waypoints;
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
    IShipCommandsHelperService _shipCommandHelperService,
    ITradesService _tradesService,
    IShipLogsService _shipLogsService,
    IAgentsService _agentsService,
    IPathsService _pathsService
) : IShipLoopsService
{
    private static SemaphoreSlim _getShipJobSemaphore = new (1, 1);
    private Task? _explorationTask = null;

    public async Task Run()
    {
        _logger.LogInformation("Ship loop started.");
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("\nShutting down gracefully...");
            e.Cancel = true; // Prevent the process from terminating immediately
            cts.Cancel();    // Trigger the token
        };

        if (!await _collectionFactory.DatabaseExists())
        {
            await _accountService.RegisterAsync();
        }

        //List<(TimeSpan ExecutionTime, DateTime ExecutionCompletionUtc)> executionAverageCalculator = [];
        while (!cts.IsCancellationRequested || !(_explorationTask ?? Task.FromResult(false)).IsCompleted)
        {
            await CheckAndHandleReset();
            await AddAgentTokenToConfiguration();
            //await JumpGateWaypointsRefresh();
            await CreateTradeModelsIfEmpty();
            var shipStatuses = await RefreshOrCreateShipStatuses();
            await SleepUntilNextShipReady(shipStatuses);

            if (cts.IsCancellationRequested)
            {
                if (_explorationTask is not null)
                {
                    await _explorationTask;
                }
                return;
            }
            if (_explorationTask is null || _explorationTask.IsCompleted)
            {
                _explorationTask = ExploreNewSystem();
            }

            var ships = shipStatuses.Select(ss => ss.Ship).ToList();
            await UpdateSystemWaypoints(ships);
            if (await BuyNewShipIfPossible(ships))
            {
                shipStatuses = (await _shipStatusesCacheService.GetAsync()).ToList();
            }

            shipStatuses = shipStatuses
                .HexadecimalSort()
                .ToList();
            var shipStatusesToDoWork = shipStatuses.Where(ss => ShipsService.GetShipCooldown(ss.Ship) is null).ToList();

            await Parallel.ForEachAsync(shipStatusesToDoWork, new ParallelOptions {MaxDegreeOfParallelism = 10}, async (shipStatusToDoWork, ct) => 
            {
                //Stopwatch processingTimeStart = Stopwatch.StartNew();
                await DoShipWork(shipStatusToDoWork, shipStatuses);
                //executionAverageCalculator.Add((processingTimeStart.Elapsed, DateTime.UtcNow));
            });
            // foreach (var shipStatusToDoWork in shipStatusesToDoWork)
            // {
            //     await DoShipWork(shipStatusToDoWork, shipStatuses);
            //     if (cts.IsCancellationRequested) break;
            // }

            await WaitForResetIfWithinOneHour();
        }
    }

    private async Task AddAgentTokenToConfiguration()
    {
        var account = await _accountService.GetAsync();
        _configuration[$"SpaceTrader:" + ConfigurationEnums.AgentToken.ToString()] = account.Token;
    }

    private async Task CreateTradeModelsIfEmpty()
    {
        await _tradesService.GetTradeModelsWithCacheAsync();
    }

    private async Task<List<ShipStatus>> RefreshOrCreateShipStatuses()
    {
        var shipStatuses = (await _shipStatusesCacheService.GetAsync()).ToList();
        if (!shipStatuses.Any())
        {
            var refreshShips = await _shipsService.GetAsync();
            foreach (var ship in refreshShips)
            {
                shipStatuses.Add(new ShipStatus(ship, "Ship Statuses reset.", DateTime.UtcNow));
                await _shipStatusesCacheService.SetAsync(shipStatuses);
            }
        }
        return shipStatuses;
    }

    private async Task WaitForResetIfWithinOneHour()
    {
        var now = DateTime.UtcNow;
        var serverStatus = await _serverStatusService.GetAsync();
        _logger.LogInformation("Time until server reset: {hours} Hours", Math.Round((serverStatus.ServerResets.Next - now).TotalHours));

        if (serverStatus.ServerResets.Next.AddHours(-1) < now)
        {
            _logger.LogInformation("Waiting for next reset.");
            var waitInMilliseconds = (int)(serverStatus.ServerResets.Next - now).TotalMilliseconds;
            await Task.Delay(waitInMilliseconds);
        }
    }

    private async Task CheckAndHandleReset()
    {
        var serverStatus = await _serverStatusService.GetAsync();
        var resetTime = serverStatus.ServerResets.Next;
        var now = DateTime.UtcNow;

        // If we are within 1 hour of the reset, or the reset just happened
        if (resetTime.AddHours(-1) < now)
        {
            // Calculate how long to wait. 
            // If resetTime is 10:00 and now is 9:50, delay is 10 minutes.
            var delayDuration = resetTime - now;

            if (delayDuration > TimeSpan.Zero)
            {
                _logger.LogInformation("Waiting {Delay} for next reset.", delayDuration);
                await Task.Delay(delayDuration.Add(new TimeSpan(0, 10, 0))); // add 10 minutes to the delay
            }

            // Now that we've waited (or if the time already passed), do the cleanup
            await _collectionFactory.DeleteDatabaseAsync();

            var successful = false;
            while (!successful)
            {
                try
                {
                    await _accountService.RegisterAsync();
                    successful = true;
                }
                catch
                {
                    await Task.Delay(new TimeSpan(0, 10, 0));
                }
            }
        }
    }

    private async Task DoShipWork(ShipStatus shipStatus, List<ShipStatus> shipStatuses)
    {
        var ship = shipStatus.Ship;
        if (ShipsService.GetShipCooldown(ship) is not null) return;

        if (ship.ShipCommand is null)
        {
            
            var shipJobsService = _shipJobsFactory.Get(shipStatus.Ship);

            
            if (shipJobsService is null)
            {
                ship = ship with { ShipCommand = null };
                shipStatus = shipStatus with { Ship = ship };
                await _shipStatusesCacheService.SetAsync(shipStatus);
                return;
            }

            ShipCommand? shipCommand = null;
            await _getShipJobSemaphore.WaitAsync();
            try
            {
                shipStatuses = (await _shipStatusesCacheService.GetAsync()).ToList();
                shipCommand = await shipJobsService.Get(shipStatuses.Select(ss => ss.Ship), shipStatus.Ship);
                ship = ship with { ShipCommand = shipCommand };
                shipStatus = shipStatus with { Ship = ship };
                await _shipStatusesCacheService.SetAsync(shipStatus);
            }
            finally
            {
                _getShipJobSemaphore.Release();
            }

            ship = ship with { ShipCommand = shipCommand };
            shipStatus = shipStatus with { Ship = ship };
            await _shipStatusesCacheService.SetAsync(shipStatus);
            if (shipCommand is null) return;
        }

            var shipCommandService = _shipCommandsServiceFactory.Get(ship.ShipCommand.ShipCommandEnum);

        try
        {
            var shipStatusesDictionary = shipStatuses.ToDictionary(ss => ss.Ship.Symbol, ss => ss.Ship);
            var newShipStatus = await shipCommandService.Run(
                shipStatus,
                shipStatusesDictionary);
            if (newShipStatus is null)
            {
                // shipStatuses.Remove(shipStatus);
                // i--;
                await _shipStatusesCacheService.DeleteAsync(shipStatus);
                shipStatus = newShipStatus;
            }
            else
            {
                shipStatus = newShipStatus;
                ship = shipStatus.Ship;
                ship = ship with { Error = null };
                shipStatus = shipStatus with { Ship = ship };
                await _shipStatusesCacheService.SetAsync(shipStatus);
            }
        }
        catch (SpaceTraderResultException ex)
        {
            await AddShipLogsError(ship, ex);
            var timeSpan = TimeSpan.FromMinutes(2);
            ship = await _shipsService.GetAsync(ship.Symbol);
            await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
            if (ship.Cooldown is null)
            {
                ship = ship with { Cooldown = new Cooldown(ship.Symbol, (int)timeSpan.TotalSeconds, (int)timeSpan.TotalSeconds, DateTime.UtcNow.Add(timeSpan)) };
            }
            ship = ship with
            {
                Error = ex.Message + " " + ex.InnerException?.Message ?? "" + " " + ex.ResponseBody,
            };
            shipStatus = shipStatus with { Ship = ship };
            await _shipStatusesCacheService.SetAsync(shipStatus);
        }
    }

    private async Task AddShipLogsError(Ship ship, Exception ex)
    {
        await _shipLogsService.AddAsync(new ShipLog(
            ship.Symbol, 
            ShipLogEnum.Error,
            JsonSerializer.Serialize(new
            {
                ship.Nav.WaypointSymbol,
                ex.Message,
                ex.StackTrace,
                ex.Data,
            }),
            DateTime.UtcNow,
            DateTime.UtcNow));
    }

    private async Task JumpGateWaypointsRefresh()
    {
        var systems = await _systemsService.GetAsync();
        var jumpGateWaypoints = systems.SelectMany(s => s.Waypoints).Where(w => w.Type == WaypointTypesEnum.JUMP_GATE.ToString() && w.JumpGate is null);
        foreach (var jumpGateWaypoint in jumpGateWaypoints)
        {
            var waypoint = await _waypointsService.GetAsync(jumpGateWaypoint.Symbol, refresh: true);
        }
    }

    private async Task<bool> BuyNewShipIfPossible(IEnumerable<Ship> ships)
    {
        var shipToBuy = await _shipCommandHelperService.ShipToBuy(ships);
        if (shipToBuy.Item1 is not null && shipToBuy.Item2 is not null)
        {
            return await _shipCommandHelperService.CheckRemotePurchaseShip(ships, shipToBuy.Item1, shipToBuy.Item2.Value);
        }
        return false;
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
            }
        }
    }

    public async Task ExploreNewSystem()
    {
        var agent = await _agentsService.GetAsync();
        var systems = await _systemsService.GetAsync();
        var links = SystemsService.GetSystemSymbolsWithinXJumps(systems, WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters), distance: 6);
        var systemsWithinXJumps = systems.Where(s => links.Contains(s.Symbol));

        var finishedJumpGate = systems.SingleOrDefault(s => s.Symbol == WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters) && s.Waypoints.Any(w => w.JumpGate is not null && !w.IsUnderConstruction));
        if (finishedJumpGate is null) return;

        //var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters));
        var paths = await _pathsService.BuildSystemPathWithCostWithBurn2(systemsWithinXJumps.Select(s => s.Symbol).ToList(), agent.Headquarters, 600, 600);
        var waypoints = systemsWithinXJumps.SelectMany(s => s.Waypoints);
        var jumpGateWaypoints = waypoints.Where(w => w.JumpGate is not null);
        var jumpGatePaths = paths
            .Where(p => jumpGateWaypoints.Select(w => w.Symbol).Contains(p.WaypointSymbol))
            .OrderBy(p => p.TimeCost);
        string? nextToRefresh = null;
        foreach (var jumpGatePath in jumpGatePaths)
        {
            var jumpGateWaypoint = jumpGateWaypoints.Single(w => w.Symbol == jumpGatePath.WaypointSymbol);
            foreach (var connection in jumpGateWaypoint.JumpGate!.Connections)
            {
                var connectionSystemSymbol = WaypointsService.ExtractSystemFromWaypoint(connection);
                var connectionSystem = systemsWithinXJumps.SingleOrDefault(s => s.Symbol == connectionSystemSymbol);
                if (!systemsWithinXJumps.Any(s => s.Symbol == connectionSystemSymbol)
                    && connectionSystem is null)
                {
                    nextToRefresh = connectionSystemSymbol;
                    break;
                }
            }
            if (nextToRefresh is not null) break;
        }

        if (nextToRefresh is null) return;
        var newSystem = await _systemsService.GetAsync(nextToRefresh, true);
        foreach (var waypoint in newSystem.Waypoints)
        {
            await _waypointsService.GetAsync(waypoint.Symbol, true);
        }
    }
}