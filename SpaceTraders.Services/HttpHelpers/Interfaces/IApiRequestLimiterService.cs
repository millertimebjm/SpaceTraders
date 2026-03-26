namespace SpaceTraders.Services.HttpHelpers.Interfaces;

public interface IApiRequestLimiterService
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct);
    Task ProcessQueueAsync(CancellationToken ct);
}