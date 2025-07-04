using SpaceTraders.Models;

namespace SpaceTraders.Services.Constructions.Interfaces;

public interface IConstructionsService
{
    Task<Construction> GetAsync(string constructionWaypoint);
    Task<SupplyResult> SupplyAsync(
        string waypointSymbol,
        string shipSymbol,
        string tradeSymbol,
        int units);
}