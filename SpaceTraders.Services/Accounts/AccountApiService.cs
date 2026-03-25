using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Models.Results;
using SpaceTraders.Services.Accounts.Interfaces;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.HttpHelpers.Interfaces;

namespace SpaceTraders.Services.Accounts;

public class AccountApiService(
    IConfiguration _configuration, 
    ILogger<AccountApiService> _logger,
    IHttpHelperService _httpHelperService) : IAccountApiService
{
    private const string _apiUrl = "https://api.spacetraders.io/v2/register";
    private string Token
    {
        get
        {
            var token = _configuration[$"SpaceTrader:" + ConfigurationEnums.AccountToken.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            return token;
        }
    }
    public async Task<AccountRegistrationResult> RegisterAsync()
    {
        var faction = "COSMIC";
        var symbol = "SPATIAL" + DateTime.Today.ToString("yyMMdd");
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { symbol, faction });
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<AccountRegistrationResult>>(
        //     _apiUrl,
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Account not registered");
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { symbol, faction });
        //var response = await _dispatcher.SendAsync(request);
        //var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        var response = await _httpHelperService.HttpSendHelper( request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Account not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<AccountRegistrationResult>>();
        return data.Datum;
    }
}