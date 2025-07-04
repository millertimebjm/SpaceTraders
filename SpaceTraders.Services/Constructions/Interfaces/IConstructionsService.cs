using SpaceTraders.Models;

namespace SpaceTraders.Services.Constructions.Interfaces;

public interface IConstructionsService
{
    Task<Construction> GetAsync(string waypointSymbol);
}