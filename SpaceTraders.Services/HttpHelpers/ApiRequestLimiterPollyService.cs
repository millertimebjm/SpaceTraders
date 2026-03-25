// using Polly;
// using Polly.RateLimit;
// using System.Net;

// public class SpaceTradersApiService
// {
//     private readonly HttpClient _httpClient;
//     private readonly AsyncPolicy<HttpResponseMessage> _resiliencePolicy;

//     public SpaceTradersApiService(HttpClient httpClient)
//     {
//         _httpClient = httpClient;

//         // 1. Define the Rate Limit: 2 requests per 1 second (burst of 2 allowed)
//         var rateLimit = Policy.RateLimitAsync<HttpResponseMessage>(2, TimeSpan.FromSeconds(1));

//         // 2. Define the Retry Policy for 429s
//         var retryPolicy = Policy
//             .HandleResult<HttpResponseMessage>(r => r.StatusCode == (HttpStatusCode)429)
//             .WaitAndRetryAsync(
//                 retryCount: 3,
//                 sleepDurationProvider: (retryCount, response, context) => 
//                 {
//                     // SpaceTraders usually sends a 'Retry-After' header in seconds
//                     var retryAfter = response.Result.Headers.RetryAfter?.Delta 
//                                      ?? TimeSpan.FromSeconds(Math.Pow(2, retryCount));
//                     return retryAfter;
//                 },
//                 onRetryAsync: async (response, timespan, retryCount, context) => {
//                     // Log the wait if you want to see why it's stalling
//                     Console.WriteLine($"Rate limited. Waiting {timespan.TotalSeconds}s...");
//                 });

//         // 3. Combine them: Retry first, then Rate Limit
//         _resiliencePolicy = Policy.WrapAsync(retryPolicy, rateLimit);
//     }

//     public async Task<string> SendRequestAsync(string url, CancellationToken ct)
//     {
//         return await _resiliencePolicy.ExecuteAsync(async () =>
//         {
//             var response = await _httpClient.GetAsync(url, ct);
//             response.EnsureSuccessStatusCode();
//             return await response.Content.ReadAsStringAsync(ct);
//         });
//     }
// }