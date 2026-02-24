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

public class ConstructionsService : IConstructionsService
{
    private readonly string _apiUrl;
    private readonly string _token;
    private readonly HttpClient _httpClient;
    private readonly ILogger<IConstructionsService> _logger;
    private readonly IShipLogsService _shipLogsService;
    public ConstructionsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<IConstructionsService> logger,
        IShipLogsService shipLogsService)
    {
        _httpClient = httpClient;
        _apiUrl = configuration[$"SpaceTrader:"+ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[$"SpaceTrader:"+ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
        _logger = logger;
        _shipLogsService = shipLogsService;
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