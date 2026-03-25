using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.HttpHelpers.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsApiService(
    IConfiguration _configuration,
    ILogger<SystemsService> _logger,
    IHttpHelperService _httpHelperService
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

    private string Token
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
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = DIRECTORY_PATH + systemSymbol
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpGetHelper<DataSingle<STSystem>>(
        //     url,
        //     _httpClient,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("System not retrieved");
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await _httpHelperService.HttpSendHelper(request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("System not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<STSystem>>();
        return data.Datum;
    }
}