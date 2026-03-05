using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models.Enums;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class AgentsController(
    IAgentsService agentsService,
    IShipStatusesCacheService _shipStatusesCacheService,
    ISystemsService _systemsService,
    IConfiguration _configuration
    ) : BaseController(agentsService, _shipStatusesCacheService, _systemsService)
{
    public async Task<IActionResult> Index()
    {
        var agents = await agentsService.GetAsync();
        return View(agents);
    }

    [Route("/agents/refresh")]
    public async Task<IActionResult> Refresh()
    {
        var agent = await agentsService.GetAsync(refresh: true);
        SessionHelper.Set(HttpContext, SessionEnum.CurrentCredits, agent.Credits);
        return Redirect(HttpContext.Request.Headers.Referer.FirstOrDefault() ?? "/");
    }

    [Route("agents/secrets")]
    public async Task<IActionResult> Secrets()
    {
        string SPACETRADER_PREFIX = "SpaceTrader:";
        var token = _configuration[SPACETRADER_PREFIX + ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        return Json(token);
    }
}
