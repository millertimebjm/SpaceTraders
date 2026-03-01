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

public class ShipyardsService(
    HttpClient _httpClient,
    IConfiguration _configuration,
    ILogger<ShipyardsService> _logger
) : IShipyardsService
{
    private string ApiUrl
    {
        get 
        {
            var apiUrl = _configuration[$"SpaceTrader:"+ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl);
            return apiUrl;
        }
    }

    private string BearerToken
    {
        get 
        {
            var apiUrl = _configuration[$"SpaceTrader:"+ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl);
            return apiUrl;
        }
    }

    public async Task<Shipyard> GetAsync(string shipyardWaypointSymbol)
    {
        var url = new UriBuilder(ApiUrl)
        {
            Path = $"/systems/{WaypointsService.ExtractSystemFromWaypoint(shipyardWaypointSymbol)}/waypoints/{shipyardWaypointSymbol}/shipyard"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BearerToken);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<Shipyard>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Shipyard not retrieved");
        return data.Datum;
    }

    public async Task<PurchaseShipResponse> PurchaseShipAsync(string waypointSymbol, string shipType)
    {
        var url = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BearerToken);
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
