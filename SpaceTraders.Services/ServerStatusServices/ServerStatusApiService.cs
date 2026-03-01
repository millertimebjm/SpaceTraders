using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using DnsClient.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.ServerStatusServices.Interfaces;

namespace SpaceTraders.Services.ServerStatusServices;

public class ServerStatusApiService(IConfiguration _configuration, HttpClient _httpClient, ILogger<ServerStatusApiService> _logger) : IServerStatusApiService
{
    private string BearerToken
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
        var url = new UriBuilder(_apiUrl);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BearerToken);
        var data = await HttpHelperService.HttpGetHelper<ServerStatus>(
            url.ToString(),
            _httpClient,
            _logger) ?? throw new HttpRequestException("Server Status error");
        return data;
    }
}