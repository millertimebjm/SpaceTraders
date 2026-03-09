using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Shipyards;

public class ShipyardsService(
    HttpClient _httpClient,
    IConfiguration _configuration,
    ILogger<ShipyardsService> _logger,
    IDispatcher _dispatcher
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

    private string Token
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
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/systems/{WaypointsService.ExtractSystemFromWaypoint(shipyardWaypointSymbol)}/waypoints/{shipyardWaypointSymbol}/shipyard"
        };
        var url = urlBuilder.ToString();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<Shipyard>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Shipyard not retrieved");
        return data.Datum;

        // var request = new HttpRequestMessage(HttpMethod.Get, url);
        // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        // var response = await _dispatcher.SendAsync(request);
        // //var response = await _httpClient.SendAsync(request);
        // if (!response.IsSuccessStatusCode) throw new HttpRequestException("Shipyard not retrieved");
        // var data = await response.Content.ReadFromJsonAsync<DataSingle<Shipyard>>();
        // return data.Datum;
    }

    public async Task<PurchaseShipResponse> PurchaseShipAsync(string waypointSymbol, string shipType)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships"
        };
        var url = urlBuilder.ToString();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);
        var content = JsonContent.Create(new { shipType, waypointSymbol });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<PurchaseShipResponse>>(
            url,
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Shipyard not retrieved");
        return data.Datum;

        // var request = new HttpRequestMessage(HttpMethod.Post, url);
        // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        // request.Content = JsonContent.Create(new { shipType, waypointSymbol });
        // var response = await _dispatcher.SendAsync(request);
        // //var response = await _httpClient.SendAsync(request);
        // if (!response.IsSuccessStatusCode) throw new HttpRequestException("Purchase Ship not retrieved");
        // var data = await response.Content.ReadFromJsonAsync<DataSingle<PurchaseShipResponse>>();
        // return data.Datum;
    }
}
