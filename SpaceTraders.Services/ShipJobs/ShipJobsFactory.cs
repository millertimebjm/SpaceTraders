using Microsoft.Extensions.DependencyInjection;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.ShipCommands;
using SpaceTraders.Services.ShipJobs.Interfaces;

namespace SpaceTraders.Services.ShipJobs;

public class ShipJobsFactory(IServiceProvider _serviceProvider) : IShipJobsFactory
{
    public IShipJobService? Get(Ship ship)
    {
        return Enum.Parse<ShipRegistrationRolesEnum>(ship.Registration.Role) switch
        {
            ShipRegistrationRolesEnum.EXCAVATOR => ExcavatorServices(ship),
            ShipRegistrationRolesEnum.HAULER => _serviceProvider.GetRequiredService<HaulerShipJobService>(),
            ShipRegistrationRolesEnum.COMMAND => _serviceProvider.GetRequiredService<CommandShipJobService>(),
            ShipRegistrationRolesEnum.SURVEYOR => _serviceProvider.GetRequiredService<SurveyorShipJobService>(),
            ShipRegistrationRolesEnum.TRANSPORT => _serviceProvider.GetRequiredService<TransportShipJobService>(),
            ShipRegistrationRolesEnum.SATELLITE => _serviceProvider.GetRequiredService<ProbeShipJobService>(),
            ShipRegistrationRolesEnum.EXPLORER => _serviceProvider.GetRequiredService<ExplorerShipJobService>(),
            _ => null
        };
    }

    public IShipJobService ExcavatorServices(Ship ship)
    {
        if (ship.Mounts.Any(m => m.Symbol.Contains("MINING"))) {
            return _serviceProvider.GetRequiredService<MiningShipJobService>();
        }
        return _serviceProvider.GetRequiredService<SiphonShipJobService>();
    }
}