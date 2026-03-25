namespace SpaceTraders.Services.HttpHelpers.Interfaces;

public interface IApiRequestLimiterService
{
    Task WaitUntilReadyAsync(CancellationToken ct = default);
}