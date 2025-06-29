using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Waypoints;

public class WaypointsService : IWaypointsService
{
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<WaypointsService> _logger;

    public WaypointsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WaypointsService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
    }

    public async Task<Waypoint> GetAsync(string waypointSymbol)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = $"v2/systems/{ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var waypointsDataString = await _httpClient.GetAsync(url.ToString());
        _logger.LogInformation("{WaypointsDataString}", await waypointsDataString.Content.ReadAsStringAsync());
        waypointsDataString.EnsureSuccessStatusCode();
        var waypointsData = await waypointsDataString.Content.ReadFromJsonAsync<DataSingle<Waypoint>>();
        if (waypointsData is null) throw new HttpRequestException("System Data not retrieved.");
        if (waypointsData.Datum is null) throw new HttpRequestException("System not retrieved");
        return waypointsData.Datum;
    }

    public async Task<IEnumerable<Waypoint>> GetByTypeAsync(
        string systemSymbol,
        string type)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = $"/v2/systems/{systemSymbol}/waypoints";
        url.Query = $"type={type}";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        // _logger.LogInformation("{url}", url.ToString());
        // var agentsDataString = await _httpClient.GetAsync(url.ToString());
        // _logger.LogInformation("{agentsDataString}", await agentsDataString.Content.ReadAsStringAsync());
        // var agentsData = await agentsDataString.Content.ReadFromJsonAsync<Data<Waypoint>>();
        // agentsDataString.EnsureSuccessStatusCode();
        // if (agentsData.DataList is null) throw new HttpRequestException("System not retrieved");
        // return agentsData.DataList;
        var data = await HttpHelperService.HttpGetHelper<Data<Waypoint>>(url.ToString(), _httpClient, _logger);
        return data.DataList;
    }

    public async Task<IEnumerable<Waypoint>> GetByTraitAsync(
        string systemSymbol,
        string trait)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = $"/v2/systems/{systemSymbol}/waypoints";
        url.Query = $"traits={trait}";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<Data<Waypoint>>(url.ToString(), _httpClient, _logger);
        return data.DataList;
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