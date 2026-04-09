using SpaceTraders.Models;

namespace SpaceTraders.Services.SystemRefresh.Interfaces;

public interface ISystemRefreshService
{
    Task<STSystem> RefreshSystem(string systemSymbol);
}