using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsService
{
    Task<Ship> Run(
        Ship ship,
        Dictionary<string, Ship> shipsDictionary);
}

