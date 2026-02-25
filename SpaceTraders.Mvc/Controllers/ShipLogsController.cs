using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipLogsController(
    IShipLogsStorageService _shipLogsStorageService,
    IAgentsService _agentsService,
    IShipStatusesCacheService _shipStatusesCacheService,
    ISystemsService _systemsService
) : BaseController(_agentsService, _shipStatusesCacheService, _systemsService)
{
    [Route("/shiplogs/")]
    public async Task<IActionResult> Index(ShipLogsFilterModel filterModel)
    {
        var viewModel = new ShipLogsViewModel(
        
            _shipLogsStorageService.GetAsync(filterModel),
            filterModel,
            _shipStatusesCacheService.GetAsync()
        );
        return View(viewModel);
    }
}
