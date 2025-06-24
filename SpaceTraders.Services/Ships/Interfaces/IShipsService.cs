using SpaceTraders.Models;

namespace SpaceTraders.Services.Ships.Interfaces;

public interface IShipsService
{
    Task<IEnumerable<Ship>> GetAsync();
    Task<Nav> OrbitAsync(string shipSymbol);
    Task<Nav> DockAsync(string shipSymbol);
    Task<Nav> TravelAsync(string waypointSymbol, string shipSymbol);
}