using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipyardsController(
    IShipyardsService _shipyardsService,
    IAgentsService _agentsService,
    IShipStatusesCacheService _shipStatusesCacheService,
    ISystemsService _systemsService,
    IWaypointsService _waypointsService
) : BaseController(_agentsService, _shipStatusesCacheService, _systemsService)
{
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

    [Route("/waypoints/{waypointSymbol}/shipyards/{shipType}/detail")]
    public async Task<IActionResult> Detail(string waypointSymbol, string shipType)
    {
        var waypoint = await _waypointsService.GetAsync(waypointSymbol);
        var shipyard = waypoint.Shipyard;
        var shipFrame = shipyard.ShipFrames.SingleOrDefault(sf => sf.Type == shipType);
        return Json(shipFrame);
    }
}
