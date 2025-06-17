using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
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
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()];
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()];
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
    }

    public async Task<STSystem> GetAsync(string systemSymbol)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = DIRECTORY_PATH + systemSymbol;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var agentsDataString = await _httpClient.GetAsync(url.ToString());
        _logger.LogInformation("{agentsDataString}", await agentsDataString.Content.ReadAsStringAsync());
        var agentsData = await agentsDataString.Content.ReadFromJsonAsync<DataSingle<STSystem>>();
        if (agentsData is null) throw new HttpRequestException("System Data not retrieved.");
        if (agentsData.Datum is null) throw new HttpRequestException("System not retrieved");
        return agentsData.Datum;
    }
}