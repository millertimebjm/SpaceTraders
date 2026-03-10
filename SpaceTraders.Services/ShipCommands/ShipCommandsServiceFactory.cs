using Microsoft.Extensions.DependencyInjection;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.ShipCommands.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class ShipCommandsServiceFactory(IServiceProvider _serviceProvider) : IShipCommandsServiceFactory
{
    public IShipCommandsService Get(ShipCommandEnum command)
    {
        return command switch
        {
            ShipCommandEnum.MiningToSellAnywhere => _serviceProvider.GetRequiredService<MiningToSellAnywhereCommand>(),
            ShipCommandEnum.SiphonToSellAnywhere => _serviceProvider.GetRequiredService<SiphonToSellAnywhereCommand>(),
            ShipCommandEnum.BuyToSell => _serviceProvider.GetRequiredService<BuyAndSellCommand>(),
            ShipCommandEnum.SupplyConstruction => _serviceProvider.GetRequiredService<SupplyConstructionCommand>(),
            ShipCommandEnum.Survey => _serviceProvider.GetRequiredService<SurveyCommand>(),
            ShipCommandEnum.PurchaseShip => _serviceProvider.GetRequiredService<PurchaseShipCommand>(),
            ShipCommandEnum.Exploration => _serviceProvider.GetRequiredService<ExplorationCommand>(),
            ShipCommandEnum.FulfillContract => _serviceProvider.GetRequiredService<FulfillContractCommand>(),
            ShipCommandEnum.MarketWatch => _serviceProvider.GetRequiredService<MarketWatchCommand>(),
            _ => throw new ArgumentException($"Unknown command: {command}", nameof(command))
        };
    }
}