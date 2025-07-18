using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class MarketplacesController : BaseController
{
    private readonly ILogger<MarketplacesController> _logger;
    private readonly IMarketplacesService _marketplacesService;

    public MarketplacesController(
        ILogger<MarketplacesController> logger,
        IMarketplacesService marketplacesService,
        IAgentsService agentsService) : base(agentsService)
    {
        _logger = logger;
        _marketplacesService = marketplacesService;
    }

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

    [Route("/marketplaces/{systemSymbol}/trademodels")]
    public async Task<IActionResult> TradeModels(string systemSymbol)
    {
        var modelTrades = await _marketplacesService.GetTradeModelsAsync();
        var orderedModelTrades = _marketplacesService.GetBestOrderedTrades(modelTrades);
        return View((systemSymbol, orderedModelTrades));
    }
}
