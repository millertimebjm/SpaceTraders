namespace SpaceTraders.Dispatcher;

public interface IDispatcher
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
    Task ProcessQueueAsync(CancellationToken token);
}