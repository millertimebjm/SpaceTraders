using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Services.Ships.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipsController : BaseController
{
    private readonly ILogger<ShipsController> _logger;
    private readonly IShipsService _shipsService;

    public ShipsController(
        ILogger<ShipsController> logger,
        IShipsService shipsService)
    {
        _logger = logger;
        _shipsService = shipsService;
    }

    [Route("/ships")]
    public async Task<IActionResult> Index()
    {
        var ships = await _shipsService.GetAsync();
        return View(ships);
    }

    [Route("/ships/{shipSymbol}/active")]
    public IActionResult SetActive(string shipSymbol)
    {
        HttpContext.Session.SetString("CurrentShipSymbol", shipSymbol);
        return RedirectToAction("Index");
    }

    [Route("/ships/{shipSymbol}/orbit")]
    public async Task<IActionResult> Orbit(string shipSymbol)
    {
        var nav = await _shipsService.OrbitAsync(shipSymbol);
        return RedirectToAction("Index");
    }

    [Route("/ships/{shipSymbol}/dock")]
    public async Task<IActionResult> Dock(string shipSymbol)
    {
        var nav = await _shipsService.DockAsync(shipSymbol);
        return RedirectToAction("Index");
    }
}
