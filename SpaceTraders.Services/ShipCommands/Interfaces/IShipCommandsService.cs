using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsService
{
    Task<DateTime?> Run(
        string shipSymbol,
        Waypoint startWaypoint,
        Waypoint endWaypoint);
}

