using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.Constructions;

public class ConstructionsService(
    HttpClient _httpClient,
    IConfiguration _configuration,
    ILogger<IConstructionsService> _logger,
    IShipLogsService _shipLogsService
) : IConstructionsService
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
            var token = _configuration[$"SpaceTrader:"+ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            return token;
        }
    }

    public async Task<Construction> GetAsync(string waypointSymbol)
    {
        var url = new UriBuilder(ApiUrl);
        url.Path = $"v2/systems/{WaypointsService.ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}/construction";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BearerToken);
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
        var url = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/systems/{WaypointsService.ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}/construction/supply"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BearerToken);
        var content = JsonContent.Create(new { shipSymbol, tradeSymbol = inventory, units });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<SupplyResult>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Supply not retrieved");
        await AddSupplyShipLog(shipSymbol, waypointSymbol, inventory, units);
        return data.Datum;
    }

    private async Task AddSupplyShipLog(string shipSymbol, string waypointSymbol, string inventory, int units)
    {
        var datetime = DateTime.UtcNow;
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.SupplyConstruction,
            JsonSerializer.Serialize(new
            {
                JumpGateWaypoint = waypointSymbol,
                InventorySymbol = inventory,
                InventoryUnits = units,
            }),
            datetime,
            datetime
        );
        await _shipLogsService.AddAsync(shipLog);
    }
}