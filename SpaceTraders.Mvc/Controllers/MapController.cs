using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class MapController(
    ISystemsService _systemsService,
    IShipStatusesCacheService _shipStatusesCacheService,
    BaseControllerDependencyInjectionContext baseControllerContext) : BaseController(baseControllerContext)
{
    public async Task<IActionResult> Index(string systemSymbol)
    {
        var model = new MapViewModel(
            _systemsService.GetAsync(systemSymbol),
            _shipStatusesCacheService.GetAsync()
        );
        return View(model);
    }

    public async Task<IActionResult> Galaxy()
    {
        var model = new GalaxyViewModel(
            _systemsService.GetAsync(),
            _shipStatusesCacheService.GetAsync()
        );
        return View(model);
    }
}