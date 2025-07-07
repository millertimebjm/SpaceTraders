using SpaceTraders.Models.Enums;

namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsServiceFactory
{
    IShipCommandsService Get(ShipCommandEnum command);
}