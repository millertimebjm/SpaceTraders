using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class MiningShipJobService : IShipJobService
{
    public Task<ShipCommand> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        return Task.FromResult(new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.MiningToSellAnywhere));
    }
}