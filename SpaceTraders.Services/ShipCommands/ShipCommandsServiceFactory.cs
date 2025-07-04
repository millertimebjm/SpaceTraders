using Microsoft.Extensions.DependencyInjection;
using SpaceTraders.Services.ShipCommands.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class ShipCommandsServiceFactory : IShipCommandsServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ShipCommandsServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IShipCommandsService Get(string commandName)
    {
        return commandName switch
        {
            "MiningToSellAnywhere" => _serviceProvider.GetRequiredService<MiningToSellAnywhereCommand>(),
            "BuyToSell" => _serviceProvider.GetRequiredService<BuyAndSellCommand>(),
            "SupplyConstruction" => _serviceProvider.GetRequiredService<SupplyConstructionCommand>(),
            _ => throw new ArgumentException($"Unknown command: {commandName}", nameof(commandName))
        };
    }
}