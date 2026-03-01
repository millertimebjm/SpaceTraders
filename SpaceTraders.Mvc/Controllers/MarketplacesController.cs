using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
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
    ISystemsService _systemsService
) : BaseController(_agentsService, _shipstatusesCacheService, _systemsService)
{
    [Route("/marketplaces/{marketplaceWaypointSymbol}")]
    public async Task<IActionResult> Index(string marketplaceWaypointSymbol)
    {
        var marketplace = await _marketplacesService.GetAsync(marketplaceWaypointSymbol);
        return View(marketplace);
    }

    // [Route("/marketplaces/{marketplaceWaypointSymbol}/buy/{inventory}")]
    // public async Task<IActionResult> Buy(string shipSymbol, string inventory)
    // {
    //     await _marketplacesService.BuyAsync(shipSymbol, inventory);
    //     return RedirectToRoute(new
    //     {
    //         controller = "Marketplaces",
    //         action = "Index",
    //         marketplaceWaypointSymbol
    //     });
    // }

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
}
