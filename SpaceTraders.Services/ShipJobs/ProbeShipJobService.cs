using SpaceTraders.Models;
using SpaceTraders.Services.ShipJobs.Interfaces;

namespace SpaceTraders.Services.ShipJobs;

public class ProbeShipJobService(

) : IShipJobService
{
    public Task<ShipCommand?> Get(IEnumerable<Ship> ships, Ship ship)
    {
        return Task.FromResult(new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.MarketWatch));
    }
}