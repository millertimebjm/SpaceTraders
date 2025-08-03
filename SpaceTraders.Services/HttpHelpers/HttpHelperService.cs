using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SpaceTraders.Model.Exceptions;
using SpaceTraders.Models;

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
        var responseString = await response.Content.ReadAsStringAsync();
        logger.LogInformation("{responseString}", responseString);
        try
        {
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<T>();
            if (data is null) throw new HttpRequestException("HttpGet returned null data.");
            return data;
        }
        catch (HttpRequestException ex)
        {
            string responseBody = null;
            if (response != null)
            {
                // Read the response body (if any) from the failed response
                responseBody = await response.Content.ReadAsStringAsync();
            }
            var detailedMessage = $"HTTP request failed. Status code: {response?.StatusCode}";
            throw new SpaceTraderResultException(detailedMessage, ex, responseBody);
        }
    }

    public async static Task HttpGetHelper(
        string url,
        HttpClient httpClient,
        ILogger logger)
    {
        logger.LogInformation("{url}", url);
        var response = await httpClient.GetAsync(url);
        logger.LogInformation("{responseString}", await response.Content.ReadAsStringAsync());
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            string responseBody = null;
            if (response != null)
            {
                // Read the response body (if any) from the failed response
                responseBody = await response.Content.ReadAsStringAsync();
            }
            var detailedMessage = $"HTTP request failed. Status code: {response?.StatusCode}";
            throw new SpaceTraderResultException(detailedMessage, ex, responseBody);
        }
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
        try
        {
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<T>();
            if (data is null) throw new HttpRequestException("HttpPost returned null data.");
            return data;
        }
        catch (HttpRequestException ex)
        {
            string responseBody = null;
            if (response != null)
            {
                // Read the response body (if any) from the failed response
                responseBody = await response.Content.ReadAsStringAsync();
            }
            var detailedMessage = $"HTTP request failed. Status code: {response?.StatusCode}";
            throw new SpaceTraderResultException(detailedMessage, ex, responseBody);
        }
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
        try
        {
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync();
            if (data is null) throw new HttpRequestException("HttpPost returned null data.");
            return data;
        }
        catch (HttpRequestException ex)
        {
            string responseBody = null;
            if (response != null)
            {
                // Read the response body (if any) from the failed response
                responseBody = await response.Content.ReadAsStringAsync();
            }
            var detailedMessage = $"HTTP request failed. Status code: {response?.StatusCode}";
            throw new SpaceTraderResultException(detailedMessage, ex, responseBody);
        }
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
        try
        {
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync();
            if (data is null) throw new HttpRequestException("HttpPost returned null data.");
            return data;
        }
        catch (HttpRequestException ex)
        {
            string responseBody = null;
            if (response != null)
            {
                // Read the response body (if any) from the failed response
                responseBody = await response.Content.ReadAsStringAsync();
            }
            var detailedMessage = $"HTTP request failed. Status code: {response?.StatusCode}";
            throw new SpaceTraderResultException(detailedMessage, ex, responseBody);
        }
    }
}