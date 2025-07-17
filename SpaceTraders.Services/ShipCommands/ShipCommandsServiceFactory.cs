using Microsoft.Extensions.DependencyInjection;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.ShipCommands.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class ShipCommandsServiceFactory : IShipCommandsServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ShipCommandsServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IShipCommandsService Get(ShipCommandEnum command)
    {
        return command switch
        {
            ShipCommandEnum.MiningToSellAnywhere => _serviceProvider.GetRequiredService<MiningToSellAnywhereCommand>(),
            ShipCommandEnum.BuyToSell => _serviceProvider.GetRequiredService<BuyAndSellCommand>(),
            ShipCommandEnum.SupplyConstruction => _serviceProvider.GetRequiredService<SupplyConstructionCommand>(),
            ShipCommandEnum.Survey => _serviceProvider.GetRequiredService<SurveyCommand>(),
            ShipCommandEnum.PurchaseShip => _serviceProvider.GetRequiredService<PurchaseShipCommand>(),
            ShipCommandEnum.Exploration => _serviceProvider.GetRequiredService<ExplorationCommand>(),
            _ => throw new ArgumentException($"Unknown command: {command}", nameof(command))
        };
    }
}