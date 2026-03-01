using System.Net.Http.Headers;
using System.Net.Http.Json;
using DnsClient.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.ServerStatusServices.Interfaces;

namespace SpaceTraders.Services.ServerStatusServices;

public class ServerStatusService(
    IServerStatusApiService _serverStatusApiService,
    IServerStatusCacheService _serverStatusCacheService) : IServerStatusService
{
    
    public async Task<ServerStatus> GetAsync()
    {
        var serverStatus = await _serverStatusCacheService.GetAsync();
        if (serverStatus is null)
        {
            serverStatus = await _serverStatusApiService.GetAsync();
            await _serverStatusCacheService.SetAsync(serverStatus);
        }
        return serverStatus;
    }
}