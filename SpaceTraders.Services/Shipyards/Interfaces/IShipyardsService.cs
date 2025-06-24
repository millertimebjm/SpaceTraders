using SpaceTraders.Models;

namespace SpaceTraders.Services.Shipyards.Interfaces;

public interface IShipyardsService
{
    Task<Shipyard> GetAsync(string systemSymbol, string shipyardWaypointSymbol);
    Task<Ship> BuyAsync(string waypointSymbol, string shipType);
}