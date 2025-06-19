using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Shipyards.Interfaces;

namespace SpaceTraders.Services.Shipyards;

public class ShipyardsService : IShipyardsService
{
    private const string DIRECTORY_PATH = "/v2/systems";
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<ShipyardsService> _logger;

    public ShipyardsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ShipyardsService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
    }

    public async Task<Shipyard> GetAsync(string systemSymbol, string shipyardWaypointSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = DIRECTORY_PATH + $"/{systemSymbol}/{shipyardWaypointSymbol}/shipyard"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<Shipyard>>(
            url.ToString(),
            _httpClient,
            _logger);
        // var shipyardsDataString = await _httpClient.GetAsync(url.ToString());
        // _logger.LogInformation("{shipyardsDataString}", await shipyardsDataString.Content.ReadAsStringAsync());
        // shipyardsDataString.EnsureSuccessStatusCode();
        // var shipyardsData = await shipyardsDataString.Content.ReadFromJsonAsync<DataSingle<Shipyard>>();
        // if (shipyardsData is null) throw new HttpRequestException("Shipyard Data not retrieved.");
        if (data.Datum is null) throw new HttpRequestException("Shipyard not retrieved");
        return data.Datum;
    }
}
