using SpaceTraders.Models;

namespace SpaceTraders.Services.Surveys.Interfaces;

public interface ISurveysCacheService
{
    Task DeleteAsync(string signature);
    Task<IEnumerable<Survey>> GetAsync();
    Task<IEnumerable<Survey>> GetAsync(string waypointSymbol);
    Task SetAsync(Survey survey);
}