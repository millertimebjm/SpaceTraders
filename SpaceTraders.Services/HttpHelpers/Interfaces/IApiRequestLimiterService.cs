namespace SpaceTraders.Services.HttpHelpers.Interfaces;

public interface IApiRequestLimiterService
{
    Task<DateTime> RequestToken(string agentId);
}