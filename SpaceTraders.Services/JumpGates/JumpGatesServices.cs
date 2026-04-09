using System.Net.Http.Headers;
using System.Net.Http.Json;
using DnsClient.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.HttpHelpers.Interfaces;
using SpaceTraders.Services.JumpGates.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.JumpGates;

public class JumpGatesServices(
    IConfiguration _configuration,
    ILogger<JumpGatesServices> _logger,
    IHttpHelperService _httpHelperService
) : IJumpGatesServices
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

    public async Task<JumpGate> GetAsync(string waypointSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"v2/systems/{WaypointsService.ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}/jump-gate"
        };
        var url = urlBuilder.ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await _httpHelperService.HttpSendHelperWithAgent(request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Account not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<JumpGate>>();
        return data.Datum;
    }
}