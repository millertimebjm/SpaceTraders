using SpaceTraders.Models;
using SpaceTraders.Models.Enums;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class ExplorerShipJobService() : IShipJobService
{
    public async Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        return new ShipCommand(ship.Symbol, ShipCommandEnum.CompleteOtherConstruction);
    }
}