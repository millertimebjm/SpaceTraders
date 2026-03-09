using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Models.Results;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.HttpHelpers;

namespace SpaceTraders.Services.Contracts;

public class ContractsApiService(
    IConfiguration _configuration,
    HttpClient _httpClient,
    ILogger<ContractsApiService> _logger,
    IDispatcher _dispatcher
) : IContractsApiService
{
    private const string DIRECTORY_PATH = "/v2/my/contracts";
    private string ApiUrl
    {
        get
        {
            return _configuration[$"SpaceTrader:"+ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        }
    }
    private string Token
    {
        get
        {
            return _configuration[$"SpaceTrader:"+ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        } 
    }

    public async Task<IEnumerable<STContract>> GetAsync()
    {
        var urlBuilder = new UriBuilder(ApiUrl);
        urlBuilder.Path = DIRECTORY_PATH;
        var page = 1;
        var url = urlBuilder.ToString() + $"?page={page}&limit=20";
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);
        var allData = new List<STContract>();
        Data<STContract> latestPull;
        
        do
        {
            latestPull = await HttpHelperService.HttpGetHelper<Data<STContract>>(
                url,
                _httpClient,
                _logger);
            allData.AddRange(latestPull.DataList);
            page++;
        } while (allData.Count < latestPull.Meta.Total);
        
        if (allData is null) throw new HttpRequestException("Contracts not retrieved");
        return allData;
    }

    public async Task<STContract?> GetActiveAsync()
    {
        var dataList = await GetAsync();
        return dataList.Where(c => c.Accepted && !c.Fulfilled).OrderByDescending(c => c.DeadlineToAccept).FirstOrDefault();
    }

    public async Task<ContractAcceptResult> AcceptAsync(string contractId)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = DIRECTORY_PATH + $"/{contractId}/" + "accept"
        };
        var url = urlBuilder.ToString();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<ContractAcceptResult>>(
            url,
            _httpClient,
            null,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Contract not retrieved");
        return data.Datum;

        // var request = new HttpRequestMessage(HttpMethod.Post, url);
        // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        // var response = await _dispatcher.SendAsync(request);
        // //var response = await _httpClient.SendAsync(request);
        // if (!response.IsSuccessStatusCode) throw new HttpRequestException("Contract not retrieved");
        // var data = await response.Content.ReadFromJsonAsync<DataSingle<ContractAcceptResult>>();
        // return data.Datum;
    }

    public async Task<ContractFulfillResult> FulfillAsync(string contractId)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = DIRECTORY_PATH + $"/{contractId}/" + "fulfill"
        };
        var url = urlBuilder.ToString();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<ContractFulfillResult>>(
            url,
            _httpClient,
            null,
            _logger);
        return data.Datum;

        // var request = new HttpRequestMessage(HttpMethod.Post, url);
        // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        // var response = await _dispatcher.SendAsync(request);
        // //var response = await _httpClient.SendAsync(request);
        // if (!response.IsSuccessStatusCode) throw new HttpRequestException("Contract not retrieved");
        // var data = await response.Content.ReadFromJsonAsync<DataSingle<ContractFulfillResult>>();
        // return data.Datum;
    }

    public async Task<ContractDeliverResult> DeliverAsync(
        string contractId,
        string shipSymbol,
        string tradeSymbol,
        int units)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = DIRECTORY_PATH + $"/{contractId}/deliver"
        };
        var url = urlBuilder.ToString();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);
        var content = JsonContent.Create(new { shipSymbol, tradeSymbol, units });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<ContractDeliverResult>>(
            url,
            _httpClient,
            content,
            _logger);
        return data.Datum;

        // var request = new HttpRequestMessage(HttpMethod.Post, url);
        // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        // var response = await _dispatcher.SendAsync(request);
        // //var response = await _httpClient.SendAsync(request);
        // if (!response.IsSuccessStatusCode) throw new HttpRequestException("Contract not retrieved");
        // var data = await response.Content.ReadFromJsonAsync<DataSingle<ContractDeliverResult>>();
        // return data.Datum;
    }

    public async Task<ContractNegotiateResult> NegotiateAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/negotiate/contract"
        };
        var url = urlBuilder.ToString();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<ContractNegotiateResult>>(
            url,
            _httpClient,
            null,
            _logger);
        return data.Datum;

        // var request = new HttpRequestMessage(HttpMethod.Post, url);
        // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        // var response = await _dispatcher.SendAsync(request);
        // //var response = await _httpClient.SendAsync(request);
        // if (!response.IsSuccessStatusCode) throw new HttpRequestException("Contract not retrieved");
        // var data = await response.Content.ReadFromJsonAsync<DataSingle<ContractNegotiateResult>>();
        // return data.Datum;
    }
}