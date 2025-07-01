using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class WaypointsController : BaseController
{
    private readonly ILogger<WaypointsController> _logger;
    private readonly IWaypointsService _waypointsService;
    private readonly IShipsService _shipsService;
    private readonly IAgentsService _agentsService;
    private readonly ISystemsService _systemsService;

    public WaypointsController(
        ILogger<WaypointsController> logger,
        IWaypointsService waypointsService,
        IShipsService shipsService,
        IAgentsService agentsService,
        ISystemsService systemsService) : base(agentsService)
    {
        _logger = logger;
        _waypointsService = waypointsService;
        _shipsService = shipsService;
        _agentsService = agentsService;
        _systemsService = systemsService;
    }

    [Route("/waypoints/{waypointSymbol}")]
    public IActionResult Index(string waypointSymbol)
    {
        var currentWaypointSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentWaypointSymbol);
        var currentWaypointTask = Task.FromResult<Waypoint?>(null);
        if (string.IsNullOrWhiteSpace(currentWaypointSymbol))
        {
            currentWaypointTask = _waypointsService.GetAsync(currentWaypointSymbol);
        }
        var currentShipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        var currentShipTask = Task.FromResult<Ship?>(null);
        if (string.IsNullOrWhiteSpace(currentShipSymbol))
        {
            currentShipTask = _shipsService.GetAsync(currentShipSymbol);
        }
        WaypointViewModel model = new(
            _waypointsService.GetAsync(waypointSymbol),
            currentWaypointTask,
            currentShipTask,
            _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(waypointSymbol))
        );
        return View(model);
    }

    [Route("/waypoints/{waypointSymbol}/reset")]
    public async Task<IActionResult> Reset(string waypointSymbol)
    {
        var waypoint = await _waypointsService.GetAsync(waypointSymbol, refresh: true);
        return Redirect($"/waypoints/{waypointSymbol}");
    }

    [Route("/waypoints/{waypointSymbol}/navigate")]
    public async Task<IActionResult> Navigate(string waypointSymbol)
    {
        var shipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        await _shipsService.NavigateAsync(waypointSymbol, shipSymbol);
        var waypoint = await _waypointsService.GetAsync(waypointSymbol);
        SessionHelper.Set(HttpContext, SessionEnum.CurrentWaypointSymbol, waypointSymbol);
        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/waypoints/{waypointSymbol}/jump")]
    public async Task<IActionResult> Jump(string waypointSymbol)
    {
        var shipSymbol = SessionHelper.Get<string?>(HttpContext, SessionEnum.CurrentShipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        var nav = await _shipsService.JumpAsync(waypointSymbol, shipSymbol);
        return RedirectToAction("Index", "Ships");
    }
}
