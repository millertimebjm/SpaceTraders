using SpaceTraders.Models;

namespace SpaceTraders.Services.Marketplaces.Interfaces;

public interface IMarketplacesService
{
    Task<Marketplace> GetAsync(string marketplaceWaypointSymbol);
    Task RefuelAsync(string shipSymbol, InventoryEnum inventory, int units);
}