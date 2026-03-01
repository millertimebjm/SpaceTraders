using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Models.Results;
using SpaceTraders.Services.Accounts.Interfaces;
using SpaceTraders.Services.HttpHelpers;

namespace SpaceTraders.Services.Accounts;

public class AccountApiService(IConfiguration _configuration, HttpClient _httpClient, ILogger<AccountApiService> _logger) : IAccountApiService
{
    private const string _apiUrl = "https://api.spacetraders.io/v2/register";
    private string BearerToken
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
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", BearerToken);
        var content = JsonContent.Create(new { symbol, faction });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<AccountRegistrationResult>>(
            _apiUrl,
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Account not registered");
        return data.Datum;
    }
}