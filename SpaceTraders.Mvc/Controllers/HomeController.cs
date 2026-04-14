using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Ships;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class HomeController(
    IAgentsService _agentsService,
    IShipStatusesCacheService _shipStatusesCacheService,
    ISystemsService _systemsService
) : BaseController(_agentsService, _shipStatusesCacheService, _systemsService)
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
