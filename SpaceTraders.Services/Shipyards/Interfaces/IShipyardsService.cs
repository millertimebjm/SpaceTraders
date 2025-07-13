using SpaceTraders.Models;

namespace SpaceTraders.Services.Shipyards.Interfaces;

public interface IShipyardsService
{
    Task<Shipyard> GetAsync(string shipyardWaypointSymbol);
    Task<PurchaseShipResponse> PurchaseShipAsync(string waypointSymbol, string shipType);
}