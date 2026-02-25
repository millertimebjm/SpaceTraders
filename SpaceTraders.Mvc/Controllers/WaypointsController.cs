using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class WaypointsController(
    ILogger<WaypointsController> _logger,
    IWaypointsService _waypointsService,
    IShipsService _shipsService,
    IAgentsService _agentsService,
    ISystemsService _systemsService,
    IShipStatusesCacheService _shipStatusesCacheService
) : BaseController(_agentsService, _shipStatusesCacheService, _systemsService)
{
    [Route("/waypoints/{waypointSymbol}")]
    public IActionResult Index(string waypointSymbol)
    {
        var currentWaypointSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentWaypointSymbol);
        var currentWaypointTask = Task.FromResult<Waypoint?>(null);
        if (!string.IsNullOrWhiteSpace(currentWaypointSymbol))
        {
            currentWaypointTask = _waypointsService.GetAsync(currentWaypointSymbol);
        }
        var currentShipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        var currentShipTask = Task.FromResult<Ship?>(null);
        if (!string.IsNullOrWhiteSpace(currentShipSymbol))
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
        var ship = await _shipsService.GetAsync(shipSymbol);
        await _shipsService.NavigateAsync(waypointSymbol, ship);
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
