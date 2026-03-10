using SpaceTraders.Models;
using SpaceTraders.Models.Enums;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public interface IShipJobsFactory
{
    IShipJobService? Get(Ship ship);
}