using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class MapController(
    IAgentsService _agentsService,
    ISystemsService _systemsService,
    IShipStatusesCacheService _shipStatusesCacheService) : BaseController(_agentsService, _shipStatusesCacheService, _systemsService)
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