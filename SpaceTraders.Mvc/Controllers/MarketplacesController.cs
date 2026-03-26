using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Trades.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Mvc.Controllers;

public class MarketplacesController(
    IMarketplacesService _marketplacesService,
    IAgentsService _agentsService,
    ITradesService _tradesService,
    ITradesCacheService _tradesCacheService,
    IShipStatusesCacheService _shipstatusesCacheService,
    ISystemsService _systemsService,
    IShipLogsService _shipLogsService,
    IShipsService _shipsService,
    IPathsService _pathsService
) : BaseController(_agentsService, _shipstatusesCacheService, _systemsService)
{
    [Route("/marketplaces/{marketplaceWaypointSymbol}")]
    public async Task<IActionResult> Index(string marketplaceWaypointSymbol)
    {
        var marketplace = await _marketplacesService.GetAsync(marketplaceWaypointSymbol);
        return View(marketplace);
    }

    [Route("/marketplaces/trademodels")]
    public async Task<IActionResult> TradeModels(string? waypointSymbol)
    {
        var agent = await _agentsService.GetAsync();
        var pathWaypointSymbol = waypointSymbol;
        if (waypointSymbol is null)
        {
            pathWaypointSymbol = agent.Headquarters;
        }
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(pathWaypointSymbol), int.MaxValue);

        var model = new TradeModelsViewModel(
            Task.Run(async () => {
                IReadOnlyList<TradeModel> orderedModelTrades;
                if (string.IsNullOrWhiteSpace(waypointSymbol))
                {
                    var tradeModels = await _tradesService.GetTradeModelsWithCacheAsync();
                    orderedModelTrades = tradeModels.OrderByDescending(tm => tm.NavigationFactor).ToList();
                }
                else
                {
                    var systems = await _systemsService.GetAsync();
                    var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(waypointSymbol));
                    var modelTrades = await _tradesService.GetTradeModelsAsyncWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), waypointSymbol, 600, 600);
                    orderedModelTrades = modelTrades.OrderByDescending(mt => mt.NavigationFactor).ToList();
                }
                return orderedModelTrades;
            }),
            _pathsService.BuildSystemPathWithCostWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), pathWaypointSymbol!, 600, 600),
            waypointSymbol ?? "");
        return View(model);
    }

    [Route("/marketplaces/trademodelsreport")]
    public async Task<IActionResult> TradeModelsReport()
    {
        var agent = await _agentsService.GetAsync();
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters), int.MaxValue);
        var waypointDictionary = traversableSystems.SelectMany(s => s.Waypoints).ToDictionary(w => w.Symbol, w => w);
        var tradeModels = await _tradesCacheService.GetTradeModelsAsync();

        List<TradeModel> badTradeModels = [];
        foreach (var tradeModel in tradeModels)
        {
            var buyWaypoint = waypointDictionary[tradeModel.ExportWaypointSymbol];
            if (buyWaypoint.Marketplace?.TradeGoods?.Any(tg => tg.Symbol == tradeModel.TradeSymbol) == false)
            {
                badTradeModels.Add(tradeModel);
                continue;
            }
            var sellWaypoint = waypointDictionary[tradeModel.ImportWaypointSymbol];
            if (sellWaypoint.Marketplace?.TradeGoods?.Any(tg => tg.Symbol == tradeModel.TradeSymbol) == false)
            {
                badTradeModels.Add(tradeModel);
                continue;
            }
        }

        var model = new TradeModelsReportViewModel(Task.FromResult(badTradeModels));
        return View(model);
    }

    [Route("/marketplaces/trademodels/reset")]
    public async Task<IActionResult> ResetTradeModels()
    {
        await _tradesService.TradeModelRefreshIfNone(refresh: true);
        return RedirectToRoute(new
        {
            controller = "Marketplaces",
            action = "TradeModels"
        });
    }

    [Route("/marketplaces/tradestats")]
    public async Task<IActionResult> TradeStats()
    {
        var shipsTask = _shipsService.GetAsync();
        var shipLogsTask = _shipLogsService.GetShipLogsForProfitAnalysisAsync();
        var shipLogsAccountData = ProcessShipLogsIntoAccountData(await shipsTask, await shipLogsTask);
        return View((shipsTask, shipLogsAccountData));
    }

    private static IEnumerable<ShipRevenueEvent> ProcessShipLogsIntoAccountData(IEnumerable<Ship> ships, IEnumerable<ShipLog> shipLogs)
    {
        var shipLogsByShip = shipLogs.GroupBy(sl => sl.ShipSymbol);
        var shipRevenueEvents = new ConcurrentBag<ShipRevenueEvent>();
        foreach (var shipLogsSet in shipLogsByShip)
        {
            var ship = ships.Single(s => s.Symbol == shipLogsSet.Key);
            var roleRevenueEvents = new List<ShipRevenueEvent>();
            if (ship.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()
                || ship.Registration.Role == ShipRegistrationRolesEnum.COMMAND.ToString())
            {
                Console.WriteLine($"Processing {ship.Symbol}");
                roleRevenueEvents = ProcessHaulerShipRevenueEvents(shipLogsSet);
                Console.WriteLine($"Found {roleRevenueEvents.Count} events");
            }
            if (ship.Registration.Role == ShipRegistrationRolesEnum.EXCAVATOR.ToString())
            {
                Console.WriteLine($"Processing {ship.Symbol}");
                roleRevenueEvents = ProcessExcavatorShipRevenueEvents(shipLogsSet);
                Console.WriteLine($"Found {roleRevenueEvents.Count} events");
            }
            foreach (var newEvent in roleRevenueEvents)
            {
                shipRevenueEvents.Add(newEvent);
            }
        }
        return shipRevenueEvents;
    }

    private static List<ShipRevenueEvent> ProcessExcavatorShipRevenueEvents(IGrouping<string, ShipLog> shipLogsSet)
    {
        var shipSymbol = shipLogsSet.Key;
        DateTime? currentStartTime = null;
        int currentRevenueAmount = 0;
        List<ShipRevenueEvent> shipRevenueEvents = [];
        foreach (var shipLog in shipLogsSet.OrderBy(sls => sls.StartedDateTimeUtc))
        {
            if (currentStartTime is not null && shipLog.ShipLogEnum == ShipLogEnum.Extract)
            {
                if (currentRevenueAmount > 0)
                {
                    shipRevenueEvents.Add(new ShipRevenueEvent(shipSymbol, currentRevenueAmount, shipLog.CompletedDateTimeUtc, shipLog.CompletedDateTimeUtc - currentStartTime.Value));
                }
                currentStartTime = null;
                currentRevenueAmount = 0;
                continue;
            }
            if (currentStartTime is null && (shipLog.ShipLogEnum == ShipLogEnum.Refuel || shipLog.ShipLogEnum == ShipLogEnum.SellCommodity))
            {
                currentStartTime = shipLog.StartedDateTimeUtc;
            }
            if (shipLog.ShipLogEnum == ShipLogEnum.Refuel)
            {
                var data = JsonSerializer.Deserialize<ShipLogTotalCredits>(shipLog.JsonData);
                currentRevenueAmount -= data.TotalCredits;
            }
            if (shipLog.ShipLogEnum == ShipLogEnum.SellCommodity)
            {
                var data = JsonSerializer.Deserialize<ShipLogTotalCredits>(shipLog.JsonData);
                currentRevenueAmount += data.TotalCredits;
            }
        }
        return shipRevenueEvents;
    }

    private static List<ShipRevenueEvent> ProcessSiphonerShipRevenueEvents(IGrouping<string, ShipLog> shipLogsSet)
    {
        var shipSymbol = shipLogsSet.Key;
        DateTime? currentStartTime = null;
        int currentRevenueAmount = 0;
        List<ShipRevenueEvent> shipRevenueEvents = [];
        foreach (var shipLog in shipLogsSet.OrderBy(sls => sls.StartedDateTimeUtc))
        {
            if (currentStartTime is not null && shipLog.ShipLogEnum == ShipLogEnum.Siphon)
            {
                if (currentRevenueAmount > 0)
                {
                    shipRevenueEvents.Add(new ShipRevenueEvent(shipSymbol, currentRevenueAmount, shipLog.CompletedDateTimeUtc, shipLog.CompletedDateTimeUtc - currentStartTime.Value));
                }
                currentStartTime = null;
                currentRevenueAmount = 0;
                continue;
            }
            if (currentStartTime is null && (shipLog.ShipLogEnum == ShipLogEnum.Refuel || shipLog.ShipLogEnum == ShipLogEnum.SellCommodity))
            {
                currentStartTime = shipLog.StartedDateTimeUtc;
            }
            if (shipLog.ShipLogEnum == ShipLogEnum.Refuel)
            {
                var data = JsonSerializer.Deserialize<ShipLogTotalCredits>(shipLog.JsonData);
                currentRevenueAmount -= data.TotalCredits;
            }
            if (shipLog.ShipLogEnum == ShipLogEnum.SellCommodity)
            {
                var data = JsonSerializer.Deserialize<ShipLogTotalCredits>(shipLog.JsonData);
                currentRevenueAmount += data.TotalCredits;
            }
        }
        return shipRevenueEvents;
    }

    private static List<ShipRevenueEvent> ProcessHaulerShipRevenueEvents(IGrouping<string, ShipLog> shipLogsSet)
    {
        var shipSymbol = shipLogsSet.Key;
        DateTime? currentStartTime = null;
        int currentRevenueAmount = 0;
        List<ShipRevenueEvent> shipRevenueEvents = [];
        var buyCommodityDone = false;
        foreach (var shipLog in shipLogsSet.OrderBy(sls => sls.StartedDateTimeUtc))
        {
            if (currentStartTime is not null && shipLog.ShipLogEnum == ShipLogEnum.BuyCommodity && buyCommodityDone)
            {
                if (currentRevenueAmount > 0)
                {
                    shipRevenueEvents.Add(new ShipRevenueEvent(shipSymbol, currentRevenueAmount, shipLog.CompletedDateTimeUtc, shipLog.CompletedDateTimeUtc - currentStartTime.Value));
                }
                currentStartTime = null;
                currentRevenueAmount = 0;
                buyCommodityDone = false;
                continue;
            }
            if (currentStartTime is null && (shipLog.ShipLogEnum == ShipLogEnum.Refuel || shipLog.ShipLogEnum == ShipLogEnum.SellCommodity || shipLog.ShipLogEnum == ShipLogEnum.BuyCommodity))
            {
                currentStartTime = shipLog.StartedDateTimeUtc;
            }
            if (shipLog.ShipLogEnum == ShipLogEnum.Refuel || shipLog.ShipLogEnum == ShipLogEnum.BuyCommodity)
            {
                var data = JsonSerializer.Deserialize<ShipLogTotalCredits>(shipLog.JsonData);
                currentRevenueAmount -= data.TotalCredits;
            }
            if (shipLog.ShipLogEnum == ShipLogEnum.SellCommodity)
            {
                var data = JsonSerializer.Deserialize<ShipLogTotalCredits>(shipLog.JsonData);
                currentRevenueAmount += data.TotalCredits;
                buyCommodityDone = true;
            }
        }
        return shipRevenueEvents;
    }

    [Route("/marketplaces/pathtester/{originWaypointSymbol?}/{destinationWaypointSymbol?}")]
    public async Task<IActionResult> PathTester(string? originWaypointSymbol, string? destinationWaypointSymbol)
    {
        var agent = await _agentsService.GetAsync();
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        
        List<PathModelWithBurn> pathsWithBurn;
        List<PathModel> paths;
        originWaypointSymbol ??= agent.Headquarters;

        pathsWithBurn = await _pathsService.BuildSystemPathWithCostWithBurn2(traversableSystems.Select(s => s.Symbol).ToList(), originWaypointSymbol, 600, 600);
        paths = PathsService.BuildSystemPathWithCost(waypoints, originWaypointSymbol, 600, 600);
  
        return View((waypoints, pathsWithBurn, paths, originWaypointSymbol, destinationWaypointSymbol));
    }

    [Route("/marketplaces/shipyards")]
    public async Task<IActionResult> Shipyards()
    {
        var agent = await _agentsService.GetAsync();
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        
        // List<PathModelWithBurn> pathsWithBurn;
        // List<PathModel> paths;
        // originWaypointSymbol ??= agent.Headquarters;

        // pathsWithBurn = PathsService.BuildSystemPathWithCostWithBurn(waypoints, originWaypointSymbol, 600, 600);
        // paths = PathsService.BuildSystemPathWithCost(waypoints, originWaypointSymbol, 600, 600);
  
        // return View((waypoints, pathsWithBurn, paths, originWaypointSymbol, destinationWaypointSymbol));

        var shipyards = waypoints.Where(w => w.Shipyard is not null).ToList();
        return View(shipyards);
    }

    [Route("/marketplaces/marketplaces")]
    public async Task<IActionResult> Marketplaces()
    {
        var agent = await _agentsService.GetAsync();
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters));
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
        
        // List<PathModelWithBurn> pathsWithBurn;
        // List<PathModel> paths;
        // originWaypointSymbol ??= agent.Headquarters;

        // pathsWithBurn = PathsService.BuildSystemPathWithCostWithBurn(waypoints, originWaypointSymbol, 600, 600);
        // paths = PathsService.BuildSystemPathWithCost(waypoints, originWaypointSymbol, 600, 600);
  
        // return View((waypoints, pathsWithBurn, paths, originWaypointSymbol, destinationWaypointSymbol));

        var marketplaces = waypoints.Where(w => w.Marketplace is not null).ToList();
        return View(marketplaces);
    }
}

public record ShipLogTotalCredits(int TotalCredits);