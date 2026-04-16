using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SpaceTraders.Models.Enums;
using SpaceTraders.Mvc.Services;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Mvc.Controllers;

public class BaseController(
    BaseControllerDependencyInjectionContext baseControllerContext) : Controller
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await HandleSessionConfigurationAgentToken();
        ViewBag.CurrentShipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        ViewBag.CurrentWaypointSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentWaypointSymbol);
        var agentTask = baseControllerContext.AgentsService.GetAsync();
        var shipStatusesTask = baseControllerContext.ShipStatusesCacheService.GetAsync();
        var systemsTask = baseControllerContext.SystemsService.GetAsync();
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

    private async Task HandleSessionConfigurationAgentToken()
    {
        var agentToken = SessionHelper.Get<string>(HttpContext, SessionEnum.AgentToken);
        var agentTokenExpiration = SessionHelper.Get<DateTime?>(HttpContext, SessionEnum.AgentTokenExpiration);
        if (agentToken is not null && agentTokenExpiration is not null && agentTokenExpiration > DateTime.UtcNow)
        {
            baseControllerContext.Configuration[$"SpaceTrader:" + ConfigurationEnums.AgentToken.ToString()] = agentToken;
            return;
        }

        var newServerStatus = await baseControllerContext.ServerStatusService.GetAsync();
        if (newServerStatus.ServerResets.Next < DateTime.UtcNow) throw new Exception($"Cache data has now be updated after reset.  Reset UTC {newServerStatus.ServerResets.Next:d} {newServerStatus.ServerResets.Next:t}");
        
        SessionHelper.Set(HttpContext, SessionEnum.AgentTokenExpiration, newServerStatus.ServerResets.Next);

        var newAccount = await baseControllerContext.AccountService.GetAsync();
        SessionHelper.Set(HttpContext, SessionEnum.AgentToken, newAccount.Token);
        baseControllerContext.Configuration[$"SpaceTrader:" + ConfigurationEnums.AgentToken.ToString()] = newAccount.Token;
    }
}