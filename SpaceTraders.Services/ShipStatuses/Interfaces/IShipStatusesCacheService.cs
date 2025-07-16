using SpaceTraders.Models;

namespace SpaceTraders.Services.Systems.Interfaces;

public interface IShipStatusesCacheService
{
    Task<IEnumerable<ShipStatus>> GetAsync();
    Task<ShipStatus> GetAsync(string shipSymbol);
    Task SetAsync(ShipStatus shipStatus);
    Task DeleteAsync();
}