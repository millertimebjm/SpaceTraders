using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.HttpHelpers;

namespace SpaceTraders.Services.Contracts;

public class ContractsService : IContractsService
{
    private const string DIRECTORY_PATH = "/v2/my/contracts";
    private readonly ILogger<ContractsService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string _token;

    public ContractsService(
        ILogger<ContractsService> logger,
        IConfiguration configuration,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
    }

    public async Task<IEnumerable<STContract>> GetAsync()
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = DIRECTORY_PATH;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<Data<STContract>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.DataList is null) throw new HttpRequestException("Contracts not retrieved");
        return data.DataList;
    }

    public async Task<STContract?> GetActiveAsync()
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = DIRECTORY_PATH;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<Data<STContract>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.DataList is null) throw new HttpRequestException("Contracts not retrieved");
        return data.DataList.OrderByDescending(c => c.Accepted && !c.Fulfilled).FirstOrDefault();
    }

    public async Task<STContract> GetAsync(string contractId)
    {
        var url = new UriBuilder(_apiUrl);
        url.Path = DIRECTORY_PATH + $"/{contractId}";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<STContract>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Contract not retrieved.");
        return data.Datum;
    }

    public async Task<STContract> AcceptAsync(string contractId)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = DIRECTORY_PATH + $"/{contractId}/" + "accept"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<STContract>>(
            url.ToString(),
            _httpClient,
            null,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Contract not retrieved");
        return data.Datum;
    }

    public async Task FulfillAsync(string contractId)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = DIRECTORY_PATH + $"/{contractId}/" + "fulfill"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper(
            url.ToString(),
            _httpClient,
            null,
            _logger);
    }

    public async Task DeliverAsync(
        string contractId,
        string shipSymbol,
        string tradeSymbol,
        int units)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = DIRECTORY_PATH + $"/{contractId}/deliver"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { shipSymbol, tradeSymbol, units });
        var data = await HttpHelperService.HttpPostHelper(
            url.ToString(),
            _httpClient,
            content,
            _logger);
    }

    public async Task NegotiateAsync(
        string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/negotiate/contract"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper(
            url.ToString(),
            _httpClient,
            null,
            _logger);
    }
}