using Microsoft.Extensions.DependencyInjection;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.ShipJobs.Interfaces;

namespace SpaceTraders.Services.ShipJobs;

public class ShipJobsFactory(IServiceProvider _serviceProvider) : IShipJobsFactory
{
    public IShipJobService? Get(ShipRegistrationRolesEnum shipRegistrationRole)
    {
        return shipRegistrationRole switch
        {
            ShipRegistrationRolesEnum.EXCAVATOR => _serviceProvider.GetRequiredService<MiningShipJobService>(),
            ShipRegistrationRolesEnum.HAULER => _serviceProvider.GetRequiredService<HaulerShipJobService>(),
            ShipRegistrationRolesEnum.COMMAND => _serviceProvider.GetRequiredService<CommandShipJobService>(),
            ShipRegistrationRolesEnum.SURVEYOR => _serviceProvider.GetRequiredService<SurveyorShipJobService>(),
            _ => null
        };
    }
    
    public IShipJobService? Get(Ship ship)
    {
        return Get((ShipRegistrationRolesEnum)Enum.Parse(typeof(ShipRegistrationRolesEnum), ship.Registration.Role));
    }
}