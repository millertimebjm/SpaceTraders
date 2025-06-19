using SpaceTraders.Models;

namespace SpaceTraders.Services.Ships.Interfaces;

public interface IShipsService
{
    Task<IEnumerable<Ship>> GetAsync();
}