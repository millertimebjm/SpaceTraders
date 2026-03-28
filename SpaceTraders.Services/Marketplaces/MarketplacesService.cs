using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.HttpHelpers.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Marketplaces;

public class MarketplacesService(
    IConfiguration _configuration,
    ILogger<MarketplacesService> _logger,
    IShipLogsService _shipLogsService,
    IHttpHelperService _httpHelperService
) : IMarketplacesService
{
    public const string SPACETRADER_PREFIX = "SpaceTrader:";
    
    private string Token
    {
        get
        {
            var token = _configuration[SPACETRADER_PREFIX + ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            return token;
        }
    }

    private string ApiUrl
    {
        get
        {
            var apiUrl = _configuration[SPACETRADER_PREFIX + ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl);
            return apiUrl;
        }
    }

    public async Task<Marketplace?> GetAsync(string marketplaceWaypointSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/systems/{WaypointsService.ExtractSystemFromWaypoint(marketplaceWaypointSymbol)}/waypoints/{marketplaceWaypointSymbol}/market"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpGetHelper<DataSingle<Marketplace>>(
        //     url,
        //     _httpClient,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Shipyard not retrieved");
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await _httpHelperService.HttpSendHelper(request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Contract not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<Marketplace>>();
        return data.Datum;
    }

    public async Task<RefuelResponse> RefuelAsync(
        string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/refuel"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { symbol = InventoryEnum.FUEL.ToString() });
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<RefuelResponse>>(
        //     url,
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        // await AddRefuelShipLog(shipSymbol, data.Datum.Transaction);
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { symbol = InventoryEnum.FUEL.ToString() });
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await _httpHelperService.HttpSendHelper(request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Ship not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<RefuelResponse>>();
        await AddRefuelShipLog(shipSymbol, data.Datum.Transaction);
        return data.Datum;
    }

    private async Task AddRefuelShipLog(string shipSymbol, MarketTransaction transaction)
    {
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.Refuel,
            JsonSerializer.Serialize(new
            {
                InventorySymbol = transaction.TradeSymbol,
                InventoryUnits = transaction.Units,
                CreditsPerUnit = transaction.PricePerUnit,
                TotalCredits = transaction.TotalPrice,
            }),
            transaction.Timestamp,
            transaction.Timestamp
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public async Task<SellCargoResponse> SellAsync(
        string shipSymbol,
        string inventory,
        int units)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/sell"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { symbol = inventory, units });
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<SellCargoResponse>>(
        //     url.ToString(),
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        // await AddSellShipLog(shipSymbol, inventory, units, data.Datum.Transaction);
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { symbol = inventory, units });
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await _httpHelperService.HttpSendHelper(request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Ship not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<SellCargoResponse>>();
        await AddSellShipLog(shipSymbol, inventory, units, data.Datum.Transaction);
        return data.Datum;
    }

    private async Task AddSellShipLog(string shipSymbol, string inventory, int units, MarketTransaction transaction)
    {
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.SellCommodity,
            JsonSerializer.Serialize(new
            {
                InventorySymbol = inventory,
                InventoryUnits = units,
                CreditsPerUnit = transaction.PricePerUnit,
                TotalCredits = transaction.TotalPrice,
            }),
            transaction.Timestamp,
            transaction.Timestamp
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public async Task<PurchaseCargoResult> PurchaseAsync(
        string shipSymbol,
        string inventory,
        int units)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/purchase"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { symbol = inventory, units });
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<PurchaseCargoResult>>(
        //     url,
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Cargo not retrieved");
        // await AddPurchaseShipLog(shipSymbol, inventory, units, data.Datum.Transaction);
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { symbol = inventory, units });
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await _httpHelperService.HttpSendHelper(request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Ship not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<PurchaseCargoResult>>();
        await AddPurchaseShipLog(shipSymbol, inventory, units, data.Datum.Transaction);
        return data.Datum;
    }

    private async Task AddPurchaseShipLog(string shipSymbol, string inventory, int units, MarketTransaction transaction)
    {
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.BuyCommodity,
            JsonSerializer.Serialize(new
            {
                InventorySymbol = inventory,
                InventoryUnits = units,
                CreditsPerUnit = transaction.PricePerUnit,
                TotalCredits = transaction.TotalPrice,
            }),
            transaction.Timestamp,
            transaction.Timestamp
        );
        await _shipLogsService.AddAsync(shipLog);
    }
}