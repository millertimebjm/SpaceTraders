using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipyardsController : BaseController
{
    private readonly ILogger<ShipyardsController> _logger;
    private readonly IShipyardsService _shipyardsService;
    
    public ShipyardsController(
        ILogger<ShipyardsController> logger,
        IShipyardsService shipyardsService,
        IAgentsService agentsService) : base(agentsService)
    {
        _logger = logger;
        _shipyardsService = shipyardsService;
    }

    [Route("/systems/{systemSymbol}/waypoints/{shipyardWaypointSymbol}/shipyard")]
    public async Task<IActionResult> Index(string systemSymbol, string shipyardWaypointSymbol)
    {
        var shipyards = await _shipyardsService.GetAsync(systemSymbol, shipyardWaypointSymbol);
        return View(shipyards);
    }

    [Route("/waypoints/{waypointSymbol}/shipyards/{shipType}/buy")]
    public async Task<IActionResult> Buy(string waypointSymbol, string shipType)
    {
        var shipyards = await _shipyardsService.BuyAsync(waypointSymbol, shipType);
        return RedirectToAction("Index", "Ships");
    }
}
