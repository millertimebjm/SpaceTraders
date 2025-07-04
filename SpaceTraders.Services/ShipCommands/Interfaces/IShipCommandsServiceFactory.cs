namespace SpaceTraders.Services.ShipCommands.Interfaces;

public interface IShipCommandsServiceFactory
{
    IShipCommandsService Get(string commandName);
}