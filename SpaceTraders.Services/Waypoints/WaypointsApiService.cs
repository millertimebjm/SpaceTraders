using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.HttpHelpers.Interfaces;
using SpaceTraders.Services.JumpGates.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Waypoints;

public class WaypointsApiService(
    IConfiguration _configuration,
    ILogger<WaypointsApiService> _logger,
    IShipyardsService _shipyardsService,
    IMarketplacesService _marketplacesService,
    IJumpGatesServices _jumpGatesService,
    IConstructionsService _constructionsService,
    IHttpHelperService _httpHelperService
) : IWaypointsApiService
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
            var token = _configuration[$"SpaceTrader:"+ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            return token;
        }
    }

    public async Task<Waypoint> GetAsync(string waypointSymbol)
    {
        var url = new UriBuilder(ApiUrl)
        {
            Path = $"v2/systems/{WaypointsService.ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}"
        };
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        //var waypointsData = await HttpHelperService.HttpGetHelper<DataSingle<Waypoint>>(url.ToString(), _httpClient, _logger);
        var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());
        var response = await _httpHelperService.HttpSendHelper(request, _logger);
        var waypointsData = await response.Content.ReadFromJsonAsync<DataSingle<Waypoint>>();
        if (waypointsData is null) throw new HttpRequestException("System Data not retrieved.");
        if (waypointsData.Datum is null) throw new HttpRequestException("System not retrieved");
        var waypoint = waypointsData.Datum;

        Task<Marketplace?> marketplaceTask = Task.FromResult<Marketplace?>(null);
        if (waypoint.Traits.Select(t => t.Symbol).Contains(WaypointTypesEnum.MARKETPLACE.ToString()))
        {
            marketplaceTask = _marketplacesService.GetAsync(waypointSymbol);
        }
        
        Task<Shipyard?> shipyardTask = Task.FromResult<Shipyard?>(null);
        if (waypoint.Traits.Select(t => t.Symbol).Contains(WaypointTypesEnum.SHIPYARD.ToString()))
        {
            shipyardTask = _shipyardsService.GetAsync(waypointSymbol);
        }

        Task<JumpGate?> jumpGateTask = Task.FromResult<JumpGate?>(null);
        if (waypoint.Type == WaypointTypesEnum.JUMP_GATE.ToString())
        {
            jumpGateTask = _jumpGatesService.GetAsync(waypointSymbol);
        }

        Task<Construction?> constructionTask = Task.FromResult<Construction?>(null);
        if (waypoint.IsUnderConstruction)
        {
            constructionTask = _constructionsService.GetAsync(waypointSymbol);
        }

        waypoint = waypoint with
        {
            Marketplace = await marketplaceTask,
            Shipyard = await shipyardTask,
            JumpGate = await jumpGateTask,
            Construction = await constructionTask
        };
        return waypoint;
    }

    public async Task<IEnumerable<Waypoint>> GetByTypeAsync(string systemSymbol, string type)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/systems/{systemSymbol}/waypoints",
            Query = $"type={type}"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpGetHelper<Data<Waypoint>>(url.ToString(), _httpClient, _logger);
        // return data.DataList;

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await _httpHelperService.HttpSendHelper(request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("System not retrieved");
        var data = await response.Content.ReadFromJsonAsync<Data<Waypoint>>();
        return data.DataList;
    }

    public async Task<IEnumerable<Waypoint>> GetByTraitAsync(string systemSymbol, string trait)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/systems/{systemSymbol}/waypoints",
            Query = $"traits={trait}"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpGetHelper<Data<Waypoint>>(url, _httpClient, _logger);
        // return data.DataList;

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await _httpHelperService.HttpSendHelper(request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("System not retrieved");
        var data = await response.Content.ReadFromJsonAsync<Data<Waypoint>>();
        return data.DataList;
    }
}