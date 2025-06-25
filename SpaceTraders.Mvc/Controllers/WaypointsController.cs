using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class WaypointsController : BaseController
{
    private readonly ILogger<WaypointsController> _logger;
    private readonly IWaypointsService _waypointsService;
    private readonly IShipsService _shipsService;

    public WaypointsController(
        ILogger<WaypointsController> logger,
        IWaypointsService waypointsService,
        IShipsService shipsService)
    {
        _logger = logger;
        _waypointsService = waypointsService;
        _shipsService = shipsService;
    }

    [Route("/waypoints/{waypointSymbol}")]
    public async Task<IActionResult> Index(string waypointSymbol)
    {
        var waypoint = await _waypointsService.GetAsync(waypointSymbol);
        return View(waypoint);
    }

    [Route("/waypoints/{waypointSymbol}/navigate")]
    public async Task<IActionResult> Navigate(string waypointSymbol)
    {
        var ship = SessionHelper.Get<Ship>(HttpContext, SessionEnum.CurrentShip);
        ArgumentException.ThrowIfNullOrWhiteSpace(ship?.Symbol);
        await _shipsService.NavigateAsync(waypointSymbol, ship.Symbol);
        var waypoint = await _waypointsService.GetAsync(waypointSymbol);
        SessionHelper.Set(HttpContext, SessionEnum.CurrentWaypoint, waypoint);
        return Redirect($"/ships/{ship.Symbol}");
    }

    [Route("/waypoints/{waypointSymbol}/jump")]
    public async Task<IActionResult> Jump(string waypointSymbol)
    {
        var ship = SessionHelper.Get<Ship?>(HttpContext, SessionEnum.CurrentShip);
        ArgumentException.ThrowIfNullOrWhiteSpace(ship?.Symbol);
        var nav = await _shipsService.JumpAsync(waypointSymbol, ship.Symbol);
        return RedirectToAction("Index", "Ships");
    }
}
