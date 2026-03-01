using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsApiService(
    HttpClient _httpClient,
    IConfiguration _configuration,
    ILogger<SystemsService> _logger
) : ISystemsApiService
{
    private const string DIRECTORY_PATH = "/v2/systems/";

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

    public async Task<STSystem> GetAsync(string systemSymbol)
    {
        var url = new UriBuilder(ApiUrl);
        url.Path = DIRECTORY_PATH + systemSymbol;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BearerToken);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<STSystem>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("System not retrieved");
        var system = data.Datum;
        return system;
    }
}