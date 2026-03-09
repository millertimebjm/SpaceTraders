using System.Threading.Channels;

namespace SpaceTraders.Dispatcher;

public class RateLimitedDispatcher(HttpClient _httpClient) : IDispatcher
{
    private readonly Channel<ApiCall> _queue = Channel.CreateUnbounded<ApiCall>();
    private readonly int MAX_BURST_CALLS = 28; // buffer for 30 max burst calls
    private readonly int BURST_RESET_IN_SECONDS = 62; // buffer for 60 second burst reset

    private DateTime? _secondWindowStartUtc = null;
    private DateTime? _burstWindowStartUtc = null;
    private int _callsInWindow = 0;
    private int _burstCallsInWindow = 0;

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var apiCall = new ApiCall(request, new());
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

    private async Task AwaitWindow()
    {
        if (_secondWindowStartUtc is null
            || (DateTime.UtcNow - _secondWindowStartUtc.Value).TotalMilliseconds > 1000)
        {
            _secondWindowStartUtc = DateTime.UtcNow;
            _callsInWindow = 1;
            return;
        }
        if (_callsInWindow < 2) // two calls per second
        {
            _callsInWindow = 2;
            return;
        }
        
        if (_burstWindowStartUtc is null
            || (DateTime.UtcNow - _burstWindowStartUtc.Value).TotalSeconds > BURST_RESET_IN_SECONDS)
        {
            _burstWindowStartUtc = DateTime.UtcNow;
            _burstCallsInWindow = 1;
            return;
        }
        if (_burstCallsInWindow < MAX_BURST_CALLS)
        {
            _burstCallsInWindow++;
        }

        // "calls per second" await with buffer after burst exceeded
        await Task.Delay(1050 - (int)(DateTime.UtcNow - _secondWindowStartUtc.Value).TotalMilliseconds); 
    }
}
