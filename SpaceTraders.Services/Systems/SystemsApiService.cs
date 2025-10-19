using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsApiService : ISystemsApiService
{
    //private const string DIRECTORY_PATH = "/v2/systems/";
    private const string DIRECTORY_PATH = "/v2/systems/";
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<SystemsService> _logger;

    public SystemsApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SystemsService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[$"SpaceTrader:"+ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[$"SpaceTrader:"+ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
    }

    public async Task<STSystem> GetAsync(string systemSymbol)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = DIRECTORY_PATH + systemSymbol;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<STSystem>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("System not retrieved");
        var system = data.Datum;
        return system;
    }
}