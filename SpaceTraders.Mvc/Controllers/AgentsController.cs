using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models.Enums;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.Agents.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class AgentsController(
    IAgentsService agentsService,
    IConfiguration _configuration,
    BaseControllerDependencyInjectionContext baseControllerContext) : BaseController(baseControllerContext)
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
