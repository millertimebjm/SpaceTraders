using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers.Interfaces;

namespace SpaceTraders.Services.HttpHelpers;

public class HttpHelperService(
    IApiRequestLimiterService _limiterService,
    IConfiguration _configuration) : IHttpHelperService
{
    private const int DELAY_IN_MILLISECONDS = 410;

    private string AgentToken
    {
        get
        {
            var token = _configuration[$"SpaceTrader:" + ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            return token;
        }
    }

    private string AccountToken
    {
        get
        {
            var token = _configuration[$"SpaceTrader:" + ConfigurationEnums.AccountToken.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            return token;
        }
    }

    public async Task<HttpResponseMessage> HttpSendHelperWithAgent(
        HttpRequestMessage request,
        ILogger logger)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AgentToken);
        return await HttpSendHelper(request, logger);
    }

    public async Task<HttpResponseMessage> HttpSendHelperWithAccount(
        HttpRequestMessage request,
        ILogger logger)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccountToken);
        return await HttpSendHelper(request, logger);
    }

    public async Task<HttpResponseMessage> HttpSendHelper(
        HttpRequestMessage request,
        ILogger logger)
    {
        logger.LogInformation("{url}", request.RequestUri);

        // var response = await _httpClient.SendAsync(request);
        // await Task.Delay(DELAY_IN_MILLISECONDS);

        HttpResponseMessage? response = null;
        try
        {
            response = await _limiterService.SendAsync(request, CancellationToken.None);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("{responseString}", await response.Content.ReadAsStringAsync());
            return response;
        }
        catch (HttpRequestException ex)
        {
            string? responseBody = null;
            if (response != null)
            {
                responseBody = await response.Content.ReadAsStringAsync();
            }
            var detailedMessage = $"HTTP request failed. Status code: {response?.StatusCode}";
            throw new SpaceTraderResultException(detailedMessage, ex, responseBody ?? "");
        }
    }
}