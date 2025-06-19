using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class WaypointsController : Controller
{
    private readonly ILogger<WaypointsController> _logger;
    private readonly IWaypointsService _waypointsService;

    public WaypointsController(
        ILogger<WaypointsController> logger,
        IWaypointsService waypointsService)
    {
        _logger = logger;
        _waypointsService = waypointsService;
    }

    [Route("/waypoints/{waypoint}")]
    public async Task<IActionResult> Index(string waypoint)
    {
        var waypoints = await _waypointsService.GetAsync(waypoint);
        return View(waypoints);
    }
}
