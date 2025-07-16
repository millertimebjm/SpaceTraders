using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
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
    private IMongoCollectionFactory _mongoCollectionFactory;

    public AgentsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AgentsService> logger,
        IMongoCollectionFactory mongoCollectionFactory)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[$"SpaceTrader:"+ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[$"SpaceTrader:"+ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
        _mongoCollectionFactory = mongoCollectionFactory;
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
            await SetAsync(agent);
        }
            
        return agent;
    }
        
    

    private async Task<Agent> GetFromApiAsync()
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = DIRECTORY_PATH;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var agentsDataString = await _httpClient.GetAsync(url.ToString());
        agentsDataString.EnsureSuccessStatusCode();
        var agentsData = await agentsDataString.Content.ReadFromJsonAsync<DataSingle<Agent>>();
        if (agentsData is null) throw new HttpRequestException("Agent Data not retrieved.");
        if (agentsData.Datum is null) throw new HttpRequestException("Agent not retrieved");
        return agentsData.Datum;
    }

    private async Task<Agent?> GetFromCacheAsync()
    {
        var collection = _mongoCollectionFactory.GetCollection<Agent>();
        var projection = Builders<Agent>.Projection.Exclude("_id");
        return await collection
            .Find(FilterDefinition<Agent>.Empty)
            .Project<Agent>(projection)
            .FirstOrDefaultAsync();
    }

    public async Task SetAsync(Agent agent)
    {
        var collection = _mongoCollectionFactory.GetCollection<Agent>();
        await collection.DeleteManyAsync(FilterDefinition<Agent>.Empty, CancellationToken.None);
        await collection.InsertOneAsync(agent, new InsertOneOptions() { }, CancellationToken.None);
    }
}
