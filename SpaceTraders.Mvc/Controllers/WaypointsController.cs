using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class WaypointsController : BaseController
{
    private readonly ILogger<WaypointsController> _logger;
    private readonly IWaypointsService _waypointsService;
    private readonly IShipsService _shipsService;

    public WaypointsController(
        ILogger<WaypointsController> logger,
        IWaypointsService waypointsService,
        IShipsService shipsService)
    {
        _logger = logger;
        _waypointsService = waypointsService;
        _shipsService = shipsService;
    }

    [Route("/waypoints/{waypoint}")]
    public async Task<IActionResult> Index(string waypoint)
    {
        var waypoints = await _waypointsService.GetAsync(waypoint);
        return View(waypoints);
    }

    [Route("/waypoints/{waypoint}/navigate")]
    public async Task<IActionResult> Navigate(string waypoint)
    {
        var shipSymbol = HttpContext.Session.GetString("CurrentShipSymbol");
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        var nav = await _shipsService.TravelAsync(waypoint, shipSymbol);
        return RedirectToAction("Index", "Ships");
    }
}
