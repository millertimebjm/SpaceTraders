using SpaceTraders.Models;

namespace SpaceTraders.Services.Marketplaces.Interfaces;

public interface IMarketplacesService
{
    Task<Marketplace> GetAsync(string marketplaceWaypointSymbol);
    Task<Cargo> PurchaseAsync(string shipSymbol, string symbol, int capacity);
    Task<Fuel> RefuelAsync(string shipSymbol);
    Task<Cargo> SellAsync(string shipSymbol, InventoryEnum inventory, int units);
    Task<Cargo> SellAsync(string shipSymbol, string inventory, int units);
}