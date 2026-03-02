using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Trades.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class MarketplacesController(
    IMarketplacesService _marketplacesService,
    IAgentsService _agentsService,
    ITradesService _tradesService,
    ITradesCacheService _tradesCacheService,
    IShipStatusesCacheService _shipstatusesCacheService,
    ISystemsService _systemsService,
    IShipLogsService _shipLogsService,
    IShipsService _shipsService
) : BaseController(_agentsService, _shipstatusesCacheService, _systemsService)
{
    [Route("/marketplaces/{marketplaceWaypointSymbol}")]
    public async Task<IActionResult> Index(string marketplaceWaypointSymbol)
    {
        var marketplace = await _marketplacesService.GetAsync(marketplaceWaypointSymbol);
        return View(marketplace);
    }

    [Route("/marketplaces/trademodels")]
    public async Task<IActionResult> TradeModels()
    {
        var modelTrades = await _tradesCacheService.GetTradeModelsAsync();
        var orderedModelTrades = _tradesService.GetBestOrderedTrades(modelTrades.ToList());
        return View(orderedModelTrades);
    }

    [Route("/marketplaces/trademodels/reset")]
    public async Task<IActionResult> ResetTradeModels()
    {
        await _tradesService.BuildTradeModel();
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
        return View(shipLogsAccountData);
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
}

public record ShipLogTotalCredits(int TotalCredits);