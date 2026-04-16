using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.ShipStatuses.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class HomeController(
    IShipStatusesCacheService _shipStatusesCacheService,
    BaseControllerDependencyInjectionContext baseControllerContext) : BaseController(baseControllerContext)
{
    public async Task<IActionResult> Index()
    {
        var shipStatuses = await _shipStatusesCacheService.GetAsync();
        return View(shipStatuses);
    }

    public async Task<IActionResult> ShipsWaiting()
    {
        var shipStatuses = await _shipStatusesCacheService.GetAsync();
        return View(shipStatuses);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
