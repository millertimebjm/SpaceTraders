using Microsoft.Extensions.Logging;

namespace SpaceTraders.Services.HttpHelpers.Interfaces;

public interface IHttpHelperService
{
    // Task<T> HttpGetHelper<T>(
    //     string url,
    //     ILogger logger);
    Task<HttpResponseMessage> HttpSendHelper(
        HttpRequestMessage request,
        ILogger logger);
}