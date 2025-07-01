using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
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
    private readonly IShipsService _shipsService;

    public SystemsController(
        ILogger<SystemsController> logger,
        ISystemsService systemsService,
        IWaypointsService waypointsService,
        IAgentsService agentsService,
        ISystemsAsyncRefreshService systemsAsyncRefreshService,
        ISystemsApiService systemsApiService,
        IShipsService shipsService) : base(agentsService)
    {
        _logger = logger;
        _systemsService = systemsService;
        _waypointsService = waypointsService;
        _systemsAsyncRefreshService = systemsAsyncRefreshService;
        _systemsApiService = systemsApiService;
        _shipsService = shipsService;
    }

    [Route("/systems/{systemSymbol}")]
    public async Task<IActionResult> Index(
        string systemSymbol,
        [FromQuery] string type,
        [FromQuery] string traits)
    {
        var currentWaypointSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentWaypointSymbol);
        Task<Waypoint?> currentWaypointTask =
            string.IsNullOrWhiteSpace(currentWaypointSymbol)
            ? Task.FromResult<Waypoint?>(null)
            : _waypointsService.GetAsync(currentWaypointSymbol);
        var currentShipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        Task<Ship?> currentShipTask =
            string.IsNullOrWhiteSpace(currentShipSymbol)
            ? Task.FromResult<Ship?>(null)
            : _shipsService.GetAsync(currentShipSymbol);
        var systemTask = _systemsService.GetAsync(systemSymbol);

        if (!string.IsNullOrWhiteSpace(type))
        {
            WaypointsViewModel model = new(
                _waypointsService.GetByTypeAsync(systemSymbol, type),
                currentWaypointTask,
                currentShipTask,
                systemTask
            );
            return View("~/Views/Waypoints/WaypointsByType.cshtml", model);
        }
        else if (!string.IsNullOrWhiteSpace(traits))
        {
            WaypointsViewModel model = new(
                _waypointsService.GetByTraitAsync(systemSymbol, traits),
                currentWaypointTask,
                currentShipTask,
                systemTask
            );
            return View("~/Views/Waypoints/WaypointsByType.cshtml", model);
        }

        var system = await systemTask;
        var currentWaypoint = await currentWaypointTask;
        if (currentWaypoint is not null)
        {
            system = system with { Waypoints = WaypointsService.SortWaypoints(system.Waypoints, currentWaypoint.X, currentWaypoint.Y).ToList() };
        }
        SystemViewModel systemModel = new(
            systemTask,
            currentShipTask,
            currentWaypointTask);

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
        var currentShipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentShipSymbol);
        var currentWaypointSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentWaypointSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentWaypointSymbol);
        var systemSymbol = WaypointsService.ExtractSystemFromWaypoint(currentWaypointSymbol);

        var systemTask = _systemsService.GetAsync(systemSymbol);
        var currentWaypointTask = _waypointsService.GetAsync(currentWaypointSymbol);
        var currentShipTask = _shipsService.GetAsync(currentShipSymbol);
        await Task.WhenAll(systemTask, currentWaypointTask, currentShipTask);
        (STSystem system, Waypoint currentWaypoint, Ship currentShip) = (await systemTask, await currentWaypointTask, await currentShipTask);

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
