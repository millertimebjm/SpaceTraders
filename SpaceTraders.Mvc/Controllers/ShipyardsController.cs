using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Services.Shipyards.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipyardsController : Controller
{
    private readonly ILogger<ShipyardsController> _logger;
    private readonly IShipyardsService _shipyardsService;

    public ShipyardsController(
        ILogger<ShipyardsController> logger,
        IShipyardsService shipyardsService)
    {
        _logger = logger;
        _shipyardsService = shipyardsService;
    }

    public async Task<IActionResult> Index(string systemSymbol, string shipyardWaypointSymbol)
    {
        var shipyards = await _shipyardsService.GetAsync(systemSymbol, shipyardWaypointSymbol);
        return View(shipyards);
    }
}
