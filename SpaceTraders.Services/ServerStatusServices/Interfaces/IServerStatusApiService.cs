using SpaceTraders.Models;

namespace SpaceTraders.Services.ServerStatusServices.Interfaces;

public interface IServerStatusApiService
{
    Task<ServerStatus> GetAsync();
}