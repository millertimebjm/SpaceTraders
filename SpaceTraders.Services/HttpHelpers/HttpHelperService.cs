using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace SpaceTraders.Services.HttpHelpers;

public static class HttpHelperService
{
    public async static Task<T> HttpGetHelper<T>(
        string url,
        HttpClient httpClient,
        ILogger logger)
    {
        logger.LogInformation("{url}", url);
        var response = await httpClient.GetAsync(url);
        logger.LogInformation("{responseString}", await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<T>();
        if (data is null) throw new HttpRequestException("HttpGet returned null data.");
        return data;
    }

    public async static Task HttpGetHelper(
        string url,
        HttpClient httpClient,
        ILogger logger)
    {
        logger.LogInformation("{url}", url);
        var response = await httpClient.GetAsync(url);
        logger.LogInformation("{responseString}", await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
        // var data = await response.Content.ReadFromJsonAsync<T>();
        // if (data is null) throw new HttpRequestException("HttpGet returned null data.");
        // return data;
    }

    internal static async Task<T> HttpPostHelper<T>(
        string url,
        HttpClient httpClient,
        HttpContent? content,
        ILogger logger)
    {
        logger.LogInformation("{url}", url);
        var response = await httpClient.PostAsync(url, content);
        logger.LogInformation("{responseString}", await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<T>();
        if (data is null) throw new HttpRequestException("HttpPost returned null data.");
        return data;
    }

    internal static async Task<string> HttpPostHelper(
        string url,
        HttpClient httpClient,
        HttpContent? content,
        ILogger logger)
    {
        logger.LogInformation("{url}", url);
        var response = await httpClient.PostAsync(url, content);
        logger.LogInformation("{responseString}", await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsStringAsync();
        if (data is null) throw new HttpRequestException("HttpPost returned null data.");
        return data;
    }

    internal static async Task<string> HttpPatchHelper(
        string url,
        HttpClient httpClient,
        HttpContent? content,
        ILogger logger)
    {
        logger.LogInformation("{url}", url);
        var response = await httpClient.PatchAsync(url, content);
        logger.LogInformation("{responseString}", await response.Content.ReadAsStringAsync());
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsStringAsync();
        if (data is null) throw new HttpRequestException("HttpPost returned null data.");
        return data;
    }
}