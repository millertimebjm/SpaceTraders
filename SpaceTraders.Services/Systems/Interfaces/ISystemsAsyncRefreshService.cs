using SpaceTraders.Models;

namespace SpaceTraders.Services.Systems.Interfaces;

public interface ISystemsAsyncRefreshService
{
    Task RefreshWaypointsAsync(STSystem system);
}