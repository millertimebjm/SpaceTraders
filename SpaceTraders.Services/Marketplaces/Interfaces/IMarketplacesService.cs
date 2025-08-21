using SpaceTraders.Models;

namespace SpaceTraders.Services.Marketplaces.Interfaces;

public interface IMarketplacesService
{
    Task<Marketplace> GetAsync(string marketplaceWaypointSymbol);
    Task<PurchaseCargoResult> PurchaseAsync(string shipSymbol, string symbol, int capacity);
    Task<RefuelResponse> RefuelAsync(string shipSymbol);
    Task<Cargo> SellAsync(string shipSymbol, InventoryEnum inventory, int units);
    Task<SellCargoResponse> SellAsync(string shipSymbol, string inventory, int units);
}