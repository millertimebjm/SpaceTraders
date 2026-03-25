using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.HttpHelpers.Interfaces;
using SpaceTraders.Services.ServerStatusServices.Interfaces;

namespace SpaceTraders.Services.ServerStatusServices;

public class ServerStatusApiService(
    IConfiguration _configuration, 
    ILogger<ServerStatusApiService> _logger,
    IHttpHelperService _httpHelperService) : IServerStatusApiService
{
    private string Token
    {
        get
        {
            var token = _configuration[$"SpaceTrader:" + ConfigurationEnums.AccountToken.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            return token;
        }
    }

    private const string _apiUrl = "https://api.spacetraders.io/v2/";

    public async Task<ServerStatus> GetAsync()
    {
        var urlBuilder = new UriBuilder(_apiUrl);
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpGetHelper<ServerStatus>(
        //     url,
        //     _httpClient,
        //     _logger) ?? throw new HttpRequestException("Server Status error");
        // return data;

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await _httpHelperService.HttpSendHelper(request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Server Status not retrieved");
        return await response.Content.ReadFromJsonAsync<ServerStatus>();
    }
}