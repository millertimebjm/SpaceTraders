using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;

namespace SpaceTraders.Services.Agents;

public class AgentsService : IAgentsService
{
    //private const string DIRECTORY_PATH = "/v2/agents";
    private const string DIRECTORY_PATH = "/v2/my/agent";
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<AgentsService> _logger;

    public AgentsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AgentsService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
    }

    public async Task<Agent> GetAsync()
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = DIRECTORY_PATH;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var agentsDataString = await _httpClient.GetAsync(url.ToString());
        _logger.LogInformation("{agentsDataString}", await agentsDataString.Content.ReadAsStringAsync());
        agentsDataString.EnsureSuccessStatusCode();
        var agentsData = await agentsDataString.Content.ReadFromJsonAsync<DataSingle<Agent>>();
        if (agentsData is null) throw new HttpRequestException("Agent Data not retrieved.");
        if (agentsData.Datum is null) throw new HttpRequestException("Agent not retrieved");
        return agentsData.Datum;
    }
}
