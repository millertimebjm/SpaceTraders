using SpaceTraders.Models;

namespace SpaceTraders.Services.Systems.Interfaces;

public interface ISystemsApiService
{
    Task<STSystem> GetAsync(string systemSymbol);
}