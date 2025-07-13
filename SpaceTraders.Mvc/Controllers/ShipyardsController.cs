using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipyardsController : BaseController
{
    private readonly ILogger<ShipyardsController> _logger;
    private readonly IShipyardsService _shipyardsService;
    private readonly IAgentsService _agentsService;

    public ShipyardsController(
        ILogger<ShipyardsController> logger,
        IShipyardsService shipyardsService,
        IAgentsService agentsService) : base(agentsService)
    {
        _logger = logger;
        _shipyardsService = shipyardsService;
        _agentsService = agentsService;
    }

    [Route("/systems/{systemSymbol}/waypoints/{shipyardWaypointSymbol}/shipyard")]
    public async Task<IActionResult> Index(string systemSymbol, string shipyardWaypointSymbol)
    {
        var shipyards = await _shipyardsService.GetAsync(shipyardWaypointSymbol);
        return View(shipyards);
    }

    [Route("/waypoints/{waypointSymbol}/shipyards/{shipType}/buy")]
    public async Task<IActionResult> Buy(string waypointSymbol, string shipType)
    {
        PurchaseShipResponse purchaseShipResponse = await _shipyardsService.PurchaseShipAsync(waypointSymbol, shipType);
        await _agentsService.SetAsync(purchaseShipResponse.Agent);
        return RedirectToAction("Index", "Ships");
    }
}
