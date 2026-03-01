using SpaceTraders.Models;

namespace SpaceTraders.Services.ServerStatusServices.Interfaces;

public interface IServerStatusService
{
    Task<ServerStatus> GetAsync();
}