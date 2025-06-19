using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Ships.Interfaces;

namespace SpaceTraders.Services.Shipyards;

public class ShipsService : IShipsService
{
    private const string DIRECTORY_PATH = "/v2/my/ships";
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<ShipsService> _logger;

    public ShipsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ShipsService> logger)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
    }

    public async Task<IEnumerable<Ship>> GetAsync()
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = DIRECTORY_PATH
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<Data<Ship>>(
            url.ToString(),
            _httpClient,
            _logger);
        // var shipsDataString = await _httpClient.GetAsync(url.ToString());
        // _logger.LogInformation("{shipsDataString}", await shipsDataString.Content.ReadAsStringAsync());
        // shipsDataString.EnsureSuccessStatusCode();
        // var shipsData = await shipsDataString.Content.ReadFromJsonAsync<Data<Ship>>();
        // if (shipsData is null) throw new HttpRequestException("Ship Data not retrieved.");
        if (data.DataList is null) throw new HttpRequestException("Ship not retrieved");
        return data.DataList;
    }
}
