using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Mvc.Services;
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

        var currentWaypoint = SessionHelper.Get<Waypoint>(HttpContext, SessionEnum.CurrentWaypoint);
        if (currentWaypoint is not null)
        {
            system = system with { Waypoints = ViewHelperService.SortWaypoints(system.Waypoints, currentWaypoint.X, currentWaypoint.Y) };
        }

        return View(system);
    }

    [Route("/systems/withinrange")]
    public async Task<IActionResult> WithinRange()
    {
        var ship = SessionHelper.Get<Ship>(HttpContext, SessionEnum.CurrentShip);
        ArgumentException.ThrowIfNullOrWhiteSpace(ship?.Symbol);
        var systemSymbol = ship.Nav.SystemSymbol;
        var system = await _systemsService.GetAsync(systemSymbol);
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        var fuelAvailable = ship.Fuel.Current;

        var waypointsFiltered = system
            .Waypoints
            .Where(w => ViewHelperService.CalculateDistance(currentWaypoint.X, currentWaypoint.Y, w.X, w.Y) < fuelAvailable)
            .ToList();
        system = system with { Waypoints = ViewHelperService.SortWaypoints(waypointsFiltered, currentWaypoint.X, currentWaypoint.Y) };

        return View("~/Views/Systems/Index.cshtml", system);
    }
}
