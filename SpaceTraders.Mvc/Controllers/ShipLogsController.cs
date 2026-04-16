using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipLogsController(
    IShipLogsStorageService _shipLogsStorageService,
    IShipStatusesCacheService _shipStatusesCacheService,
    BaseControllerDependencyInjectionContext baseControllerContext) : BaseController(baseControllerContext)
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
