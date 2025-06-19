using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsService : ISystemsService
{
    //private const string DIRECTORY_PATH = "/v2/systems/";
    private const string DIRECTORY_PATH = "/v2/systems/";
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<SystemsService> _logger;

    public SystemsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SystemsService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
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
        // var systemsDataString = await _httpClient.GetAsync(url.ToString());
        // _logger.LogInformation("{systemsDataString}", await systemsDataString.Content.ReadAsStringAsync());
        // systemsDataString.EnsureSuccessStatusCode();
        // var systemsData = await systemsDataString.Content.ReadFromJsonAsync<DataSingle<STSystem>>();
        // if (systemsData is null) throw new HttpRequestException("System Data not retrieved.");
        if (data.Datum is null) throw new HttpRequestException("System not retrieved");
        return data.Datum;
    }
}