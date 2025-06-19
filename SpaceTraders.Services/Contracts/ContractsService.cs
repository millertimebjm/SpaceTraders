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
        // var contractsDataString = await _httpClient.GetAsync(url.ToString());
        // _logger.LogInformation("{agentsDataString}", await contractsDataString.Content.ReadAsStringAsync());
        // contractsDataString.EnsureSuccessStatusCode();
        // var contractsData = await contractsDataString.Content.ReadFromJsonAsync<Data<STContract>>();
        // if (contractsData is null) throw new HttpRequestException("Contract Data not retrieved.");
        if (data.DataList is null) throw new HttpRequestException("Contracts not retrieved");
        return data.DataList;
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

        // var contractsDataString = await _httpClient.PostAsync(url.ToString(), null);
        // _logger.LogInformation("{agentsDataString}", await contractsDataString.Content.ReadAsStringAsync());
        // contractsDataString.EnsureSuccessStatusCode();
        // var contractsData = await contractsDataString.Content.ReadFromJsonAsync<DataSingle<STContract>>();
        // if (contractsData is null) throw new HttpRequestException("Contract Data not retrieved.");
        if (data.Datum is null) throw new HttpRequestException("Contract not retrieved");
        return data.Datum;
    }
}