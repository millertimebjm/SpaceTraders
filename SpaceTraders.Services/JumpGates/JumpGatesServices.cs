using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.JumpGates.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.JumpGates;

public class JumpGatesServices(
    HttpClient _httpClient,
    IConfiguration _configuration
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

    private string BearerToken
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
        var url = new UriBuilder(ApiUrl);
        url.Path = $"v2/systems/{WaypointsService.ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}/jump-gate";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BearerToken);
        var waypointsDataString = await _httpClient.GetAsync(url.ToString());
        waypointsDataString.EnsureSuccessStatusCode();
        var waypointsData = await waypointsDataString.Content.ReadFromJsonAsync<DataSingle<JumpGate>>();
        if (waypointsData is null) throw new HttpRequestException("System Data not retrieved.");
        if (waypointsData.Datum is null) throw new HttpRequestException("System not retrieved");
        return waypointsData.Datum;
    }
}