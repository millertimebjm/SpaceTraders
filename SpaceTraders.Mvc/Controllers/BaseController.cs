using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SpaceTraders.Mvc;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

public class BaseController(
    IAgentsService _agentsService,
    IShipStatusesCacheService _shipStatusCacheService,
    ISystemsService _systemsService
    ) : Controller
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ViewBag.CurrentShipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        ViewBag.CurrentWaypointSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentWaypointSymbol);
        var agentTask = _agentsService.GetAsync();
        var shipStatusesTask = _shipStatusCacheService.GetAsync();
        var systemsTask = _systemsService.GetAsync();
        await Task.WhenAll(agentTask, shipStatusesTask, systemsTask);
        ViewBag.CurrentCredits = agentTask.Result.Credits;
        ViewBag.ShipStatuses = shipStatusesTask.Result;
        ViewBag.Systems = systemsTask.Result;
        await base.OnActionExecutionAsync(context, next);
    }
}