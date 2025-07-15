using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Marketplaces;

public class MarketplacesService : IMarketplacesService
{
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<MarketplacesService> _logger;

    public MarketplacesService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MarketplacesService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
    }

    public async Task<Marketplace> GetAsync(string marketplaceWaypointSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/systems/{WaypointsService.ExtractSystemFromWaypoint(marketplaceWaypointSymbol)}/waypoints/{marketplaceWaypointSymbol}/market"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<Marketplace>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Shipyard not retrieved");
        return data.Datum;
    }

    public async Task<RefuelResponse> RefuelAsync(
        string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/refuel"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { symbol = InventoryEnum.FUEL.ToString() });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<RefuelResponse>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        return data.Datum;
    }

    public async Task<SellCargoResponse> SellAsync(
        string shipSymbol,
        string inventory,
        int units)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/sell"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { symbol = inventory, units });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<SellCargoResponse>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        return data.Datum;
    }

    public async Task<Cargo> SellAsync(
        string shipSymbol,
        InventoryEnum inventory,
        int units)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/sell"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { symbol = inventory.ToString(), units });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        return data.Datum.Cargo;
    }

    public async Task<PurchaseCargoResult> PurchaseAsync(
        string shipSymbol,
        string inventory,
        int units)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/purchase"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { symbol = inventory, units });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<PurchaseCargoResult>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Result not retrieved");
        return data.Datum;
    }
}
