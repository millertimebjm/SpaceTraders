using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class SystemsController : BaseController
{
    private readonly ILogger<SystemsController> _logger;
    private readonly ISystemsService _systemsService;
    private readonly IWaypointsService _waypointsService;

    public SystemsController(
        ILogger<SystemsController> logger,
        ISystemsService agentsService,
        IWaypointsService waypointsService)
    {
        _logger = logger;
        _systemsService = agentsService;
        _waypointsService = waypointsService;
    }

    [Route("/systems/{systemSymbol}")]
    public async Task<IActionResult> Index(
        string systemSymbol,
        [FromQuery] string type,
        [FromQuery] string traits)
    {
        if (!string.IsNullOrWhiteSpace(type))
        {
            var waypoints = await _waypointsService.GetByTypeAsync(systemSymbol, type);
            return View("~/Views/Waypoints/WaypointsByType.cshtml", waypoints);
        }
        else if (!string.IsNullOrWhiteSpace(traits))
        {
            var waypoints = await _waypointsService.GetByTraitAsync(systemSymbol, traits);
            return View("~/Views/Waypoints/WaypointsByType.cshtml", waypoints);
        }
        var system = await _systemsService.GetAsync(systemSymbol);
        return View(system);
    }
}
