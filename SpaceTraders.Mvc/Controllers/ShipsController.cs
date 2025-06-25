using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipsController : BaseController
{
    private readonly ILogger<ShipsController> _logger;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsService _waypointsService;

    public ShipsController(
        ILogger<ShipsController> logger,
        IShipsService shipsService,
        IWaypointsService waypointsService)
    {
        _logger = logger;
        _shipsService = shipsService;
        _waypointsService = waypointsService;
    }

    [Route("/ships")]
    public async Task<IActionResult> Index()
    {
        var ships = await _shipsService.GetAsync();
        return View(ships);
    }

    [Route("/ships/{shipSymbol}/active")]
    public async Task<IActionResult> SetActive(string shipSymbol)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        SessionHelper.Set(HttpContext, SessionEnum.CurrentShip, ship);
        var waypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        SessionHelper.Set(HttpContext, SessionEnum.CurrentWaypoint, waypoint);
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Ship",
            shipSymbol
        });
    }

    [Route("/ships/{shipSymbol}/orbit")]
    public async Task<IActionResult> Orbit(string shipSymbol)
    {
        var nav = await _shipsService.OrbitAsync(shipSymbol);
        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/{shipSymbol}/dock")]
    public async Task<IActionResult> Dock(string shipSymbol)
    {
        var nav = await _shipsService.DockAsync(shipSymbol);
        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/extract")]
    public async Task<IActionResult> Extract()
    {
        var ship = SessionHelper.Get<Ship>(HttpContext, SessionEnum.CurrentShip);
        ArgumentException.ThrowIfNullOrWhiteSpace(ship?.Symbol);
        await _shipsService.ExtractAsync(ship.Symbol);
        return Redirect($"/ships/{ship.Symbol}");
    }

    [Route("/ships/{shipSymbol}")]
    public async Task<IActionResult> Ship(string shipSymbol)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        return View(ship);
    }

    [Route("/ships/{shipSymbol}/jettison/{inventorySymbol}")]
    public async Task<IActionResult> Jettison(string shipSymbol, string inventorySymbol)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        await _shipsService.JettisonAsync(shipSymbol, inventorySymbol, ship.Cargo.Inventory.Single(i => i.Symbol == inventorySymbol).Units);
        ship = await _shipsService.GetAsync(shipSymbol);
        return Redirect($"/ships/{ship.Symbol}");
    }

    [Route("/ships/deactivate")]
    public IActionResult Deactivate()
    {
        SessionHelper.Unset(HttpContext, SessionEnum.CurrentShip);
        SessionHelper.Unset(HttpContext, SessionEnum.CurrentWaypoint);
        return RedirectToAction("Index");
    }
}
