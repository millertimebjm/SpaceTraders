using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Shipyards;

public class ShipyardsService : IShipyardsService
{
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<ShipyardsService> _logger;

    public ShipyardsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ShipyardsService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
    }

    public async Task<Shipyard> GetAsync(string shipyardWaypointSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/systems/{WaypointsService.ExtractSystemFromWaypoint(shipyardWaypointSymbol)}/waypoints/{shipyardWaypointSymbol}/shipyard"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<Shipyard>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Shipyard not retrieved");
        return data.Datum;
    }

    public async Task<PurchaseShipResponse> PurchaseShipAsync(string waypointSymbol, string shipType)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { shipType, waypointSymbol });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<PurchaseShipResponse>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Shipyard not retrieved");
        return data.Datum;
    }
}
