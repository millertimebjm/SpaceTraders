using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Mvc.Controllers;

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
        ViewBag.Agent = agentTask.Result;
        ViewBag.CurrentCredits = agentTask.Result.Credits;
        ViewBag.ShipStatuses = shipStatusesTask.Result;
        ViewBag.Systems = systemsTask.Result;
        var headquartersSystemJumpGate = systemsTask
            .Result
            .Single(s => s.Symbol == WaypointsService.ExtractSystemFromWaypoint(agentTask.Result.Headquarters))
            .Waypoints
            .SingleOrDefault(w => w.JumpGate is not null && w.IsUnderConstruction);
        if (headquartersSystemJumpGate is not null)
        {
            ViewBag.JumpGateWaypoint = headquartersSystemJumpGate;
        }
        await base.OnActionExecutionAsync(context, next);
    }
}