using SpaceTraders.Models;

namespace SpaceTraders.Services.Systems.Interfaces;

public interface ISystemsService
{
    Task<STSystem> GetAsync(string systemSymbol);
}