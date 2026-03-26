using System.Threading.Channels;
using SpaceTraders.Services.HttpHelpers.Interfaces;

namespace SpaceTraders.Services.HttpHelpers;

public class ApiRequestLimiterChannelService(HttpClient _httpClient) : IApiRequestLimiterService
{
    private readonly Channel<ApiCall> _queue = Channel.CreateUnbounded<ApiCall>();
    //private readonly int MAX_BURST_CALLS = 28; // buffer for 30 max burst calls
    //private readonly int BURST_RESET_IN_SECONDS = 62; // buffer for 60 second burst reset

    private DateTime? _secondWindowStartUtc = null;
    //private DateTime? _burstWindowStartUtc = null;
    private int _callsInWindow = 0;
    //private int _burstCallsInWindow = 0;

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var apiCall = new ApiCall(request, new(), ct);
        await _queue.Writer.WriteAsync(apiCall);
        return await apiCall.TaskCompletionSource.Task;
    }

    public async Task ProcessQueueAsync(CancellationToken token)
    {
        await foreach (var apiCall in _queue.Reader.ReadAllAsync(token))
        {
            await AwaitWindow();
            try
            {
                var response = await _httpClient.SendAsync(apiCall.Request, token);
                apiCall.TaskCompletionSource.SetResult(response);
            }
            catch (Exception ex)
            {
                apiCall.TaskCompletionSource.SetException(ex);
            }
        }
    }

    // private async Task AwaitWindow()
    // {
    //     if (_secondWindowStartUtc is null
    //         || (DateTime.UtcNow - _secondWindowStartUtc.Value).TotalMilliseconds > 1000)
    //     {
    //         _secondWindowStartUtc = DateTime.UtcNow;
    //         _callsInWindow = 1;
    //         return;
    //     }
    //     if (_callsInWindow < 2) // two calls per second
    //     {
    //         _callsInWindow = 2;
    //         return;
    //     }
        
    //     // if (_burstWindowStartUtc is null
    //     //     || (DateTime.UtcNow - _burstWindowStartUtc.Value).TotalSeconds > BURST_RESET_IN_SECONDS)
    //     // {
    //     //     _burstWindowStartUtc = DateTime.UtcNow;
    //     //     _burstCallsInWindow = 1;
    //     //     return;
    //     // }
    //     // if (_burstCallsInWindow < MAX_BURST_CALLS)
    //     // {
    //     //     _burstCallsInWindow++;
    //     // }

    //     // "calls per second" await with buffer after burst exceeded
    //     await Task.Delay(1050 - (int)(DateTime.UtcNow - _secondWindowStartUtc.Value).TotalMilliseconds); 
    //}

    private async Task AwaitWindow()
    {
        var now = DateTime.UtcNow;

        // 1. If we are in a new second, reset the counter
        if (_secondWindowStartUtc == null || (now - _secondWindowStartUtc.Value).TotalMilliseconds >= 1000)
        {
            _secondWindowStartUtc = now;
            _callsInWindow = 1;
            return;
        }

        // 2. If we still have room in this 1-second window, increment and proceed
        if (_callsInWindow < 2)
        {
            _callsInWindow++;
            return;
        }

        // 3. Window is full. Calculate how long to wait until the NEXT second starts
        var elapsed = (DateTime.UtcNow - _secondWindowStartUtc.Value).TotalMilliseconds;
        int delay = (int)(1000 - elapsed) + 10; // +10ms buffer for safety

        if (delay > 0)
        {
            await Task.Delay(delay);
        }

        // 4. After waiting, we are effectively starting a new window
        _secondWindowStartUtc = DateTime.UtcNow;
        _callsInWindow = 1;
    }
}

public record ApiCall(
    HttpRequestMessage Request, 
    TaskCompletionSource<HttpResponseMessage> TaskCompletionSource,
    CancellationToken Ct);