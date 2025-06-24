using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class BaseController : Controller
{
    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ViewBag.CurrentShipSymbol = HttpContext.Session.GetString("CurrentShipSymbol");
        return base.OnActionExecutionAsync(context, next);
    }
}