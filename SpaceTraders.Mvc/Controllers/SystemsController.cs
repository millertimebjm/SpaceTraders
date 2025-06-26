using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class SystemsController : BaseController
{
    private readonly ILogger<SystemsController> _logger;
    private readonly ISystemsService _systemsService;
    private readonly IWaypointsService _waypointsService;
    private readonly IAgentsService _agentsService;

    public SystemsController(
        ILogger<SystemsController> logger,
        ISystemsService systemsService,
        IWaypointsService waypointsService,
        IAgentsService agentsService) : base(agentsService)
    {
        _logger = logger;
        _systemsService = systemsService;
        _waypointsService = waypointsService;
        _agentsService = agentsService;
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
            system = system with { Waypoints = WaypointsService.SortWaypoints(system.Waypoints, currentWaypoint.X, currentWaypoint.Y).ToList() };
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
            .Where(w => WaypointsService.CalculateDistance(currentWaypoint.X, currentWaypoint.Y, w.X, w.Y) < fuelAvailable)
            .ToList();
        system = system with { Waypoints = WaypointsService.SortWaypoints(waypointsFiltered, currentWaypoint.X, currentWaypoint.Y).ToList() };

        return View("~/Views/Systems/Index.cshtml", system);
    }
}
