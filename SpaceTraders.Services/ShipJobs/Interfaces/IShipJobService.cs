using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public interface IShipJobService
{
    Task<ShipCommand?> Get(IEnumerable<Ship> ships, Ship ship);
}