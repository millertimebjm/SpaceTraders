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

    public static string ExtractSystemFromWaypoint(string waypointSymbol)
    {
        return waypointSymbol[..waypointSymbol.IndexOf('-', 3)];
    }
}