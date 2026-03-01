using SpaceTraders.Models;

namespace SpaceTraders.Services.ServerStatusServices.Interfaces;

public interface IServerStatusCacheService
{
    Task<ServerStatus?> GetAsync();
    Task SetAsync(ServerStatus serverStatus);
}