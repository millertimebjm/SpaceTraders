using Polly;
using SpaceTraders.Services.HttpHelpers.Interfaces;
using System.Net;

namespace SpaceTraders.Services.HttpHelpers;

public class ApiRequestLimiterPollyService : IApiRequestLimiterService
{
    private readonly HttpClient _httpClient;
    private readonly AsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public ApiRequestLimiterPollyService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _resiliencePolicy = Policy.RateLimitAsync<HttpResponseMessage>(
            numberOfExecutions: 2,
            perTimeSpan: TimeSpan.FromMilliseconds(1100),
            maxBurst: int.MaxValue
        );
    }

    public Task ProcessQueueAsync(CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        return await _resiliencePolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.SendAsync(request, ct);
            return response;
        });
    }
}