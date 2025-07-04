using System.Text;
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
    private readonly ISystemsApiService _systemsApiService;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsApiService _waypointsApiService;
    private readonly ISystemsCacheService _systemsCacheService;

    public SystemsController(
        ILogger<SystemsController> logger,
        ISystemsService systemsService,
        IWaypointsService waypointsService,
        IAgentsService agentsService,
        ISystemsApiService systemsApiService,
        IShipsService shipsService,
        IWaypointsApiService waypointsApiService,
        ISystemsCacheService systemsCacheService) : base(agentsService)
    {
        _logger = logger;
        _systemsService = systemsService;
        _waypointsService = waypointsService;
        _systemsApiService = systemsApiService;
        _shipsService = shipsService;
        _waypointsApiService = waypointsApiService;
        _systemsCacheService = systemsCacheService;
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
            system = system with { Waypoints = WaypointsService.SortWaypoints(system.Waypoints, currentWaypoint.X, currentWaypoint.Y, currentWaypoint.Symbol).ToList() };
        }
        SystemViewModel systemModel = new(
            systemTask,
            currentShipTask,
            currentWaypointTask);

        return View(systemModel);
    }

    [Route("/systems/{systemSymbol}/reset")]
    public async Task Reset(string systemSymbol)
    {
        Response.ContentType = "text/plain";
        HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");

        // force immediate rendering
        var padding = new string(' ', 1024) + "\n";
        var paddingBytes = Encoding.UTF8.GetBytes(padding);
        await Response.Body.WriteAsync(paddingBytes, 0, paddingBytes.Length);
        await Response.Body.FlushAsync();

        var system = await _systemsApiService.GetAsync(systemSymbol);
        var message = $"{DateTime.Now} - System has been retrieved.\n";
        var buffer = Encoding.UTF8.GetBytes(message);
        await Response.Body.WriteAsync(buffer, 0, buffer.Length);
        await Response.Body.FlushAsync();

        List<Waypoint> waypointsHydrated = new();
        var index = 1;
        var systemWaypointsCount = system.Waypoints.Count();
        foreach (var waypointSkeleton in system.Waypoints)
        {
            var completed = false;
            while (!completed)
            {
                try
                {
                    var waypointHydrated = await _waypointsApiService.GetAsync(waypointSkeleton.Symbol);
                    waypointsHydrated.Add(waypointHydrated);
                    completed = true;
                    message = $"{DateTime.Now} - Waypoint retrieved ({index}/{systemWaypointsCount}).\n";
                    buffer = Encoding.UTF8.GetBytes(message);
                    await Response.Body.WriteAsync(buffer, 0, buffer.Length);
                    await Response.Body.FlushAsync();
                }
                catch (HttpRequestException)
                {
                    _logger.LogError("Waypoint/Shipyard/Marketplace Rate Limit error in {type} SystemsController RefreshSystem", nameof(SystemsController));
                    await Task.Delay(5000);
                }
            }

            // Wait one second for 429-Rate Limit issues
            await Task.Delay(1000);
            index++;
        }
        system = system with { Waypoints = waypointsHydrated };
        await _systemsCacheService.SetAsync(system);

        message = "Process complete.\n";
        buffer = Encoding.UTF8.GetBytes(message);
        await Response.Body.WriteAsync(buffer, 0, buffer.Length);
        await Response.Body.FlushAsync();
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
        system = system with { Waypoints = WaypointsService.SortWaypoints(waypointsFiltered, currentWaypoint.X, currentWaypoint.Y, currentWaypoint?.Symbol).ToList() };

        SystemViewModel model = new(
            Task.FromResult(system),
            Task.FromResult<Ship?>(currentShip),
            Task.FromResult<Waypoint?>(currentWaypoint)
        );
        return View("~/Views/Systems/Index.cshtml", model);
    }
}
