using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Services.Ships.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipsController : Controller
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

    public async Task<IActionResult> Index()
    {
        var ships = await _shipsService.GetAsync();
        return View(ships);
    }
}
