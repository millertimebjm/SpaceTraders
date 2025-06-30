using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class SystemsController : BaseController
{
    private readonly ILogger<SystemsController> _logger;
    private readonly ISystemsService _systemsService;
    private readonly IWaypointsService _waypointsService;
    private readonly ISystemsAsyncRefreshService _systemsAsyncRefreshService;
    private readonly ISystemsApiService _systemsApiService;

    public SystemsController(
        ILogger<SystemsController> logger,
        ISystemsService systemsService,
        IWaypointsService waypointsService,
        IAgentsService agentsService,
        ISystemsAsyncRefreshService systemsAsyncRefreshService,
        ISystemsApiService systemsApiService) : base(agentsService)
    {
        _logger = logger;
        _systemsService = systemsService;
        _waypointsService = waypointsService;
        _systemsAsyncRefreshService = systemsAsyncRefreshService;
        _systemsApiService = systemsApiService;
    }

    [Route("/systems/{systemSymbol}")]
    public async Task<IActionResult> Index(
        string systemSymbol,
        [FromQuery] string type,
        [FromQuery] string traits)
    {
        var currentWaypoint = SessionHelper.Get<Waypoint>(HttpContext, SessionEnum.CurrentWaypoint);
        var currentShip = SessionHelper.Get<Ship>(HttpContext, SessionEnum.CurrentShip);

        if (!string.IsNullOrWhiteSpace(type))
        {
            WaypointsViewModel model = new(
                _waypointsService.GetByTypeAsync(systemSymbol, type),
                Task.FromResult(currentWaypoint),
                Task.FromResult(currentShip)
            );
            return View("~/Views/Waypoints/WaypointsByType.cshtml", model);
        }
        else if (!string.IsNullOrWhiteSpace(traits))
        {
            WaypointsViewModel model = new(
                _waypointsService.GetByTraitAsync(systemSymbol, traits),
                Task.FromResult(currentWaypoint),
                Task.FromResult(currentShip)
            );
            return View("~/Views/Waypoints/WaypointsByType.cshtml", model);
        }
        var system = await _systemsService.GetAsync(systemSymbol);

        if (currentWaypoint is not null)
        {
            system = system with { Waypoints = WaypointsService.SortWaypoints(system.Waypoints, currentWaypoint.X, currentWaypoint.Y).ToList() };
        }
        SystemViewModel systemModel = new(
            Task.FromResult(system),
            Task.FromResult(currentShip),
            Task.FromResult(currentWaypoint));

        return View(systemModel);
    }

    [Route("/systems/{systemSymbol}/reset")]
    public async Task<IActionResult> Reset(string systemSymbol)
    {
        var system = await _systemsApiService.GetAsync(systemSymbol);
        await _systemsAsyncRefreshService.RefreshWaypointsAsync(system);
        return Content("System caching completed.");
    }

    [Route("/systems/withinrange")]
    public async Task<IActionResult> WithinRange()
    {
        var currentShip = SessionHelper.Get<Ship>(HttpContext, SessionEnum.CurrentShip);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentShip?.Symbol);
        var systemSymbol = currentShip.Nav.SystemSymbol;
        var system = await _systemsService.GetAsync(systemSymbol);
        var currentWaypoint = await _waypointsService.GetAsync(currentShip.Nav.WaypointSymbol);
        var fuelAvailable = currentShip.Fuel.Current;

        var waypointsFiltered = system
            .Waypoints
            .Where(w => WaypointsService.CalculateDistance(currentWaypoint.X, currentWaypoint.Y, w.X, w.Y) < fuelAvailable)
            .ToList();
        system = system with { Waypoints = WaypointsService.SortWaypoints(waypointsFiltered, currentWaypoint.X, currentWaypoint.Y).ToList() };

        SystemViewModel model = new(
            Task.FromResult(system),
            Task.FromResult<Ship?>(currentShip),
            Task.FromResult<Waypoint?>(currentWaypoint)
        );
        return View("~/Views/Systems/Index.cshtml", model);
    }
}
