using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SpaceTraders.Models;
using SpaceTraders.Mvc;

public class BaseController : Controller
{
    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ViewBag.CurrentShip = SessionHelper.Get<Ship>(HttpContext, SessionEnum.CurrentShip);
        ViewBag.CurrentWaypoint = SessionHelper.Get<Waypoint>(HttpContext, SessionEnum.CurrentWaypoint);
        return base.OnActionExecutionAsync(context, next);
    }
}