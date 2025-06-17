using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class AgentsController : Controller
{
    private readonly ILogger<AgentsController> _logger;
    private readonly IAgentsService _agentsService;

    public AgentsController(
        ILogger<AgentsController> logger,
        IAgentsService agentsService)
    {
        _logger = logger;
        _agentsService = agentsService;
    }

    public async Task<IActionResult> Index()
    {
        var agents = await _agentsService.GetAsync();
        return View(agents);
    }
}
