using Microsoft.Extensions.Logging;

namespace SpaceTraders.Services.HttpHelpers.Interfaces;

public interface IHttpHelperService
{
    Task<HttpResponseMessage> HttpSendHelperWithAgent(
        HttpRequestMessage request,
        ILogger logger);

    Task<HttpResponseMessage> HttpSendHelperWithAccount(
        HttpRequestMessage request,
        ILogger logger);

    Task<HttpResponseMessage> HttpSendHelper(
        HttpRequestMessage request,
        ILogger logger);
}