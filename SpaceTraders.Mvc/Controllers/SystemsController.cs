using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class SystemsController : Controller
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
    public async Task<IActionResult> Index(string systemSymbol)
    {
        var system = await _systemsService.GetAsync(systemSymbol);
        return View(system);
    }

    [Route("/systems/{systemSymbol}/{type}")]
    public async Task<IActionResult> WaypointsByType(string systemSymbol, string type)
    {
        var waypoints = await _waypointsService.GetByTypeAsync(systemSymbol, type);
        return View("~/Views/Waypoints/WaypointsByType.cshtml", waypoints);
    }
}
