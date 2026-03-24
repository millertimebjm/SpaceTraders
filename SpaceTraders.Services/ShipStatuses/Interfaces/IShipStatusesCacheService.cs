using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipStatuses.Interfaces;

public interface IShipStatusesCacheService
{
    Task<IEnumerable<ShipStatus>> GetAsync();
    Task<ShipStatus> GetAsync(string shipSymbol);
    Task SetAsync(ShipStatus shipStatus);
    Task SetAsync(List<ShipStatus> shipStatuses);
}