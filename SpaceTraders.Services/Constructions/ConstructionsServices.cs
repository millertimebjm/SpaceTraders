using System.Net.Http.Headers;
using System.Net.Http.Json;
using DnsClient.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Constructions;

public class ConstructionsService : IConstructionsService
{
    private readonly string _apiUrl;
    private readonly string _token;
    private readonly HttpClient _httpClient;
    private readonly ILogger<IConstructionsService> _logger;
    public ConstructionsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<IConstructionsService> logger)
    {
        _httpClient = httpClient;
        _apiUrl = configuration[$"SpaceTrader:"+ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[$"SpaceTrader:"+ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
        _logger = logger;
    }

    public async Task<Construction> GetAsync(string waypointSymbol)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = $"v2/systems/{WaypointsService.ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}/construction";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<Construction>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Construction not retrieved");
        return data.Datum;
    }

    public async Task<SupplyResult> SupplyAsync(
        string waypointSymbol,
        string shipSymbol,
        string inventory,
        int units)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/systems/{WaypointsService.ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}/construction/supply"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { shipSymbol, tradeSymbol = inventory, units });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<SupplyResult>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Supply not retrieved");
        return data.Datum;
    }
}