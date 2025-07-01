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

public class WaypointsApiService : IWaypointsApiService
{
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<WaypointsApiService> _logger;
    private readonly IShipyardsService _shipyardsService;
    private readonly IMarketplacesService _marketplacesService;

    public WaypointsApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WaypointsApiService> logger,
        IShipyardsService shipyardsService,
        IMarketplacesService marketplacesService
    )
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
        _shipyardsService = shipyardsService;
        _marketplacesService = marketplacesService;
    }

    public async Task<Waypoint> GetAsync(string waypointSymbol)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = $"v2/systems/{WaypointsService.ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var waypointsDataString = await _httpClient.GetAsync(url.ToString());
        waypointsDataString.EnsureSuccessStatusCode();
        var waypointsData = await waypointsDataString.Content.ReadFromJsonAsync<DataSingle<Waypoint>>();
        if (waypointsData is null) throw new HttpRequestException("System Data not retrieved.");
        if (waypointsData.Datum is null) throw new HttpRequestException("System not retrieved");
        var waypoint = waypointsData.Datum;

        Task<Marketplace?> marketplaceTask = Task.FromResult<Marketplace?>(null);
        Task<Shipyard?> shipyardTask = Task.FromResult<Shipyard?>(null);
        if (waypoint.Traits.Select(t => t.Symbol).Contains(WaypointTypesEnum.MARKETPLACE.ToString()))
        {
            marketplaceTask = _marketplacesService.GetAsync(waypointSymbol);
        }
        if (waypoint.Traits.Select(t => t.Symbol).Contains(WaypointTypesEnum.SHIPYARD.ToString()))
        {
            shipyardTask = _shipyardsService.GetAsync(waypointSymbol);
        }
        waypoint = waypoint with { Marketplace = await marketplaceTask, Shipyard = await shipyardTask };

        return waypoint;
    }

    public async Task<IEnumerable<Waypoint>> GetByTypeAsync(string systemSymbol, string type)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = $"/v2/systems/{systemSymbol}/waypoints";
        url.Query = $"type={type}";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<Data<Waypoint>>(url.ToString(), _httpClient, _logger);
        return data.DataList;
    }

    public async Task<IEnumerable<Waypoint>> GetByTraitAsync(string systemSymbol, string trait)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = $"/v2/systems/{systemSymbol}/waypoints";
        url.Query = $"traits={trait}";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<Data<Waypoint>>(url.ToString(), _httpClient, _logger);
        return data.DataList;
    }
}