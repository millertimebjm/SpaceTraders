using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipLogsController(
    IShipLogsStorageService _shipLogsStorageService,
    IAgentsService _agentsService
) : BaseController(_agentsService)
{
    [Route("/shiplogs/")]
    public async Task<IActionResult> Index(ShipLogsFilterModel filterModel)
    {
        var viewModel = new ShipLogsViewModel(
        
            _shipLogsStorageService.GetAsync(filterModel),
            filterModel
        );
        return View(viewModel);
    }
}
