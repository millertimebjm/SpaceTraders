using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsService
{
    Task<ShipStatus> Run(
        ShipStatus ShipStatus,
        Dictionary<string, Ship> shipsDictionary);
}

