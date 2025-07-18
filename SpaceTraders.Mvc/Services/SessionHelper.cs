using System.Text.Json;

namespace SpaceTraders.Mvc;

public enum SessionEnum
{
    CurrentShipSymbol,
    CurrentWaypointSymbol,
    CurrentCredits,
    SystemWaypoints,
    Waypoint,
}

public static class SessionHelper
{


    public static void Set(HttpContext context, SessionEnum name, object item)
    {
        context.Session.SetString(name.ToString(), JsonSerializer.Serialize(item));
    }

    public static void Set(HttpContext context, string name, object item)
    {
        context.Session.SetString(name.ToString(), JsonSerializer.Serialize(item));
    }

    public static void Unset(HttpContext context, SessionEnum name)
    {
        context.Session.SetString(name.ToString(), string.Empty);
    }

    public static T? Get<T>(HttpContext context, SessionEnum name)
    {
        var itemString = context.Session.GetString(name.ToString());
        if (string.IsNullOrWhiteSpace(itemString)) return default;
        var item = JsonSerializer.Deserialize<T>(itemString);
        return item;
    }

    public static T? Get<T>(HttpContext context, string name)
    {
        var itemString = context.Session.GetString(name.ToString());
        if (string.IsNullOrWhiteSpace(itemString)) return default;
        var item = JsonSerializer.Deserialize<T>(itemString);
        return item;
    }
}