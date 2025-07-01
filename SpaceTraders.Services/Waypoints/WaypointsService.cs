using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Waypoints;

public class WaypointsService : IWaypointsService
{
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<WaypointsService> _logger;
    private readonly IWaypointsCacheService _waypointsCacheService;
    private readonly IWaypointsApiService _waypointsApiService;
    private readonly IMarketplacesService _marketplacesService;
    private readonly IShipyardsService _shipyardsService;

    public WaypointsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WaypointsService> logger,
        IWaypointsCacheService waypointsCacheService,
        IWaypointsApiService waypointsApiService,
        IMarketplacesService marketplacesService,
        IShipyardsService shipyardsService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _waypointsApiService = waypointsApiService;
        _waypointsCacheService = waypointsCacheService;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
        _marketplacesService = marketplacesService;
        _shipyardsService = shipyardsService;
    }

    public async Task<Waypoint> GetAsync(
        string waypointSymbol,
        bool refresh = false)
    {
        Waypoint? waypoint;
        if (!refresh)
        {
            waypoint = await _waypointsCacheService.GetAsync(waypointSymbol);
            if (waypoint is not null) return waypoint;
            _logger.LogWarning("Cache miss: {waypointName}:{waypointSymbol}", nameof(Waypoint), waypointSymbol);
        }
        waypoint = await _waypointsApiService.GetAsync(waypointSymbol);
        await _waypointsCacheService.SetAsync(waypoint);
        return waypoint;
    }

    public async Task<IEnumerable<Waypoint>> GetByTypeAsync(
        string systemSymbol,
        string type)
    {
        var waypoints = await _waypointsCacheService.GetByTypeAsync(systemSymbol, type);
        if (waypoints is not null) return waypoints;
        _logger.LogWarning("Cache miss GetByType: {type}:{systemSymbol} {waypointType}", nameof(STSystem), systemSymbol, type);
    
        waypoints = await _waypointsApiService.GetByTypeAsync(systemSymbol, type);
        return waypoints;
    }

    public async Task<IEnumerable<Waypoint>> GetByTraitAsync(
        string systemSymbol,
        string trait)
    {
        var waypoints = await _waypointsCacheService.GetByTraitAsync(systemSymbol, trait);
        if (waypoints is not null) return waypoints;
        _logger.LogWarning("Cache miss GetByTrait: {type}:{systemSymbol} {waypointTrait}", nameof(STSystem), systemSymbol, trait);
    
        waypoints = await _waypointsApiService.GetByTypeAsync(systemSymbol, trait);
        return waypoints;
    }

    public static string ExtractSystemFromWaypoint(string waypointSymbol)
    {
        return waypointSymbol[..waypointSymbol.IndexOf('-', 3)];
    }

    public static IOrderedEnumerable<Waypoint> SortWaypoints(IReadOnlyList<Waypoint> waypoints, int currentX, int currentY)
    {
        return waypoints.OrderBy(w => CalculateDistance(w.X, w.Y, currentX, currentY));
    }

    public static double CalculateDistance(double x1, double y1, double x2, double y2)
    {
        // Using the distance formula: sqrt((x2 - x1)^2 + (y2 - y1)^2)
        double deltaX = x2 - x1;
        double deltaY = y2 - y1;

        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }
}