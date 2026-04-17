using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Mvc.Controllers;

public class MapController(
    ISystemsService _systemsService,
    IShipStatusesCacheService _shipStatusesCacheService,
    IAgentsService _agentsService,
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

    [Route("/map/galaxy/{startingSystem}")]
    public async Task<IActionResult> Galaxy(string startingSystem)
    {
        if (string.IsNullOrWhiteSpace(startingSystem))
        {
            var agent = await _agentsService.GetAsync();
            startingSystem = WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters);
        }
        var model = new GalaxyViewModel(
            _systemsService.GetAsync(),
            _shipStatusesCacheService.GetAsync(),
            startingSystem
        );
        return View(model);
    }
}