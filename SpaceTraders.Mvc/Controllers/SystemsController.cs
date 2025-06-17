using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class SystemsController : Controller
{
    private readonly ILogger<SystemsController> _logger;
    private readonly ISystemsService _systemsService;

    public SystemsController(
        ILogger<SystemsController> logger,
        ISystemsService agentsService)
    {
        _logger = logger;
        _systemsService = agentsService;
    }

    [Route("/systems/{systemSymbol}")]
    public async Task<IActionResult> Index(string systemSymbol)
    {
        var system = await _systemsService.GetAsync(systemSymbol);
        return View(system);
    }
}
