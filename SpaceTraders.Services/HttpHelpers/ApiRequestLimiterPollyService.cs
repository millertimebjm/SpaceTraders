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

        // 1. Define the Rate Limit: 2 requests per 1 second (burst of 2 allowed)
        var spaceTradersPolicy = Policy.RateLimitAsync<HttpResponseMessage>(
            numberOfExecutions: 2,
            perTimeSpan: TimeSpan.FromSeconds(1), 
            maxBurst: 30
        );

        // 2. Define the Retry Policy for 429s
        var retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => r.StatusCode == (HttpStatusCode)429)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: (retryCount, response, context) => 
                {
                    // SpaceTraders usually sends a 'Retry-After' header in seconds
                    var retryAfter = response.Result.Headers.RetryAfter?.Delta 
                                     ?? TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                    return retryAfter;
                },
                onRetryAsync: async (response, timespan, retryCount, context) => {
                    // Log the wait if you want to see why it's stalling
                    Console.WriteLine($"Rate limited. Waiting {timespan.TotalSeconds}s...");
                });

        // 3. Combine them: Retry first, then Rate Limit
        _resiliencePolicy = Policy.WrapAsync(retryPolicy, spaceTradersPolicy);
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