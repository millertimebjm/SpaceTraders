using SpaceTraders.Models;

namespace SpaceTraders.Services.Marketplaces.Interfaces;

public interface IMarketplacesService
{
    Task<Marketplace> GetAsync(string marketplaceWaypointSymbol);
    Task RefuelAsync(string shipSymbol);
    Task SellAsync(string shipSymbol, InventoryEnum inventory, int units);
    Task SellAsync(string shipSymbol, string inventory, int units);
}