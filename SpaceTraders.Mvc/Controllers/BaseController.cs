using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SpaceTraders.Mvc;
using SpaceTraders.Services.Agents.Interfaces;

public class BaseController : Controller
{
    private readonly IAgentsService _agentsService;
    public BaseController(IAgentsService agentsService)
    {
        _agentsService = agentsService;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ViewBag.CurrentShipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        ViewBag.CurrentWaypointSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentWaypointSymbol);
        var credits = SessionHelper.Get<long?>(HttpContext, SessionEnum.CurrentCredits);
        if (credits is null)
        {
            var agent = await _agentsService.GetAsync();
            SessionHelper.Set(HttpContext, SessionEnum.CurrentCredits, agent.Credits);
            credits = agent.Credits;
        }
        ViewBag.CurrentCredits = credits;
        await base.OnActionExecutionAsync(context, next);
    }
}