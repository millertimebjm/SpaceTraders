using System.Net.Http.Headers;
using System.Net.Http.Json;
using DnsClient.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.MongoCache.Interfaces;

namespace SpaceTraders.Services.Agents;

public class AgentsService(
    ILogger<AgentsService> _logger,
    IConfiguration _configuration,
    HttpClient _httpClient,
    IAgentsCacheService _agentsCacheService,
    IDispatcher _dispatcher) : IAgentsService
{
    private const string DIRECTORY_PATH = "/v2/my/agent";

    private string ApiUrl
    {
        get
        {
            return _configuration[$"SpaceTrader:"+ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        }
    }
    private string Token
    {
        get
        {
            return _configuration[$"SpaceTrader:"+ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        } 
    }

    public async Task<Agent> GetAsync(bool refresh = false)
    {
        Agent? agent = null;
        if (!refresh)
        {
            agent = await GetFromCacheAsync();
        }

        if (agent is null)
        {
            agent = await GetFromApiAsync();
            await _agentsCacheService.SetAsync(agent);
        }
            
        return agent;
    }
        
    private async Task<Agent> GetFromApiAsync()
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = DIRECTORY_PATH
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var agentsData = await HttpHelperService.HttpGetHelper<DataSingle<Agent>>(url, _httpClient, _logger);
        // if (agentsData is null) throw new HttpRequestException("Agent Data not retrieved.");
        // if (agentsData.Datum is null) throw new HttpRequestException("Agent not retrieved");
        // return agentsData.Datum;

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Agent not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<Agent>>();
        return data.Datum;
    }

    private async Task<Agent?> GetFromCacheAsync()
    {
        return await _agentsCacheService.GetAsync();
    }

    public async Task SetAsync(Agent agent)
    {
        await _agentsCacheService.SetAsync(agent);
    }
}
