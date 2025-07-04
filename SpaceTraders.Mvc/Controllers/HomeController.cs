using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;

    public HomeController(
        ILogger<HomeController> logger,
        IAgentsService agentsService,
        IShipStatusesCacheService shipStatusesCacheService) : base(agentsService)
    {
        _logger = logger;
        _shipStatusesCacheService = shipStatusesCacheService;
    }

    public async Task<IActionResult> Index()
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
