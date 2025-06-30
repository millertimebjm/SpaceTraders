using System.Collections.Immutable;
using System.Threading.Tasks;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Services;

public class ViewHelperService
{
    private readonly ISystemsService _systemsService;
    private readonly IWaypointsService _waypointsService;
    private readonly IMarketplacesService _marketplacesService;
    private readonly IShipyardsService _shipyardsService;
    private readonly IShipsService _shipsService;
    private readonly IAgentsService _agentsService;

    public ViewHelperService(
        ISystemsService systemsService,
        IWaypointsService waypointsService,
        IMarketplacesService marketplacesService,
        IShipyardsService shipyardsService,
        IShipsService shipsService,
        IAgentsService agentsService
    )
    {
        _systemsService = systemsService;
        _waypointsService = waypointsService;
        _marketplacesService = marketplacesService;
        _shipyardsService = shipyardsService;
        _shipsService = shipsService;
        _agentsService = agentsService;
    }

    public static string MinimalHumanReadableTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan == TimeSpan.Zero)
            return "0s";

        if (timeSpan.Days > 0)
            return $"{timeSpan.Days}d";
        if (timeSpan.Hours > 0)
            return $"{timeSpan.Hours}h";
        if (timeSpan.Minutes > 0)
            return $"{timeSpan.Minutes}m";
        return $"{timeSpan.Seconds}s";
    }

    public static string HumanReadableTimeSpan(TimeSpan t)
    {
        if (t.TotalSeconds <= 1)
        {
            return $@"{t:s\.ff} seconds";
        }
        if (t.TotalMinutes <= 1)
        {
            return $@"{t:%s} seconds";
        }
        if (t.TotalHours <= 1)
        {
            return $@"{t:%m} minutes";
        }
        if (t.TotalDays <= 1)
        {
            return $@"{t:%h} hours";
        }

        return $@"{t:%d} days";
    }

    public static string ReadableCreditValue(long l)
    {
        if (l > 1000000000) // billion
        {
            return (l / 1000000000) + "b";
        }
        if (l > 1000000) // million
        {
            return (l / 1000000) + "m";
        }
        if (l > 1000) // thousand
        {
            return (l / 1000) + "k";
        }
        return l.ToString();
    }

    public static async Task<IReadOnlyList<Waypoint>> GetWaypoints(
        HttpContext context,
        ISystemsService systemsService,
        string systemSymbol)
    {
        var waypoints = SessionHelper.Get<IReadOnlyList<Waypoint>>(context, SessionEnum.SystemWaypoints);
        if (waypoints is not null)
        {
            return waypoints;
        }

        var system = await systemsService.GetAsync(systemSymbol);
        SessionHelper.Set(context, SessionEnum.SystemWaypoints, system.Waypoints);
        return system.Waypoints;
    }

    public static async Task<Waypoint> GetWaypoint(
        HttpContext context,
        IWaypointsService waypointsService,
        string waypointSymbol
        )
    {
        var waypoint = SessionHelper.Get<Waypoint>(context, SessionEnum.Waypoint);
        if (waypoint is not null)
        {
            return waypoint;
        }

        waypoint = await waypointsService.GetAsync(waypointSymbol);
        SessionHelper.Set(context, SessionEnum.Waypoint, waypoint);
        return waypoint;
    }

    public async Task<Dictionary<string, (Waypoint, Marketplace?, Shipyard?)>> GetWaypoints(
        HttpContext context,
        string systemSymbol
    )
    {
        var systemWaypoints = SessionHelper.Get<Dictionary<string, (Waypoint, Marketplace?, Shipyard?)>>(context, SessionEnum.SystemWaypoints.ToString() + $"-{systemSymbol}");
        if (systemWaypoints is null)
        {
            var system = await _systemsService.GetAsync(systemSymbol);
            return await UpdateWaypoints(context, system.Waypoints.Select(w => w.Symbol).ToList());
        }

        return systemWaypoints;
    }

    public async Task<Dictionary<string, (Waypoint, Marketplace?, Shipyard?)>> UpdateWaypoints(
        HttpContext context,
        IReadOnlyList<string> waypointSymbols
    )
    {
        Dictionary<string, (Waypoint, Marketplace?, Shipyard?)> waypoints = new();
        foreach (var waypointSymbol in waypointSymbols)
        {
            var waypoint = await _waypointsService.GetAsync(waypointSymbol);
            Marketplace? marketplace = null;
            Shipyard? shipyard = null;
            if (waypoint.Traits.Any(t => t.Symbol == WaypointTypesEnum.MARKETPLACE.ToString()))
            {
                marketplace = await _marketplacesService.GetAsync(waypointSymbol);
            }
            if (waypoint.Traits.Any(t => t.Symbol == WaypointTypesEnum.SHIPYARD.ToString()))
            {
                shipyard = await _shipyardsService.GetAsync(waypointSymbol);
            }
            waypoints.Add(waypointSymbol, (waypoint, marketplace, shipyard));
        }
        return waypoints;
    }

    public async Task UpdateWaypointsFromShipLocations(
        HttpContext context,
        string systemSymbol
    )
    {
        var waypoints = GetWaypoints(context, systemSymbol);
        var ships = await _shipsService.GetAsync();
        await UpdateWaypoints(context, ships.Select(s => s.Nav.WaypointSymbol).ToList());
    }
}