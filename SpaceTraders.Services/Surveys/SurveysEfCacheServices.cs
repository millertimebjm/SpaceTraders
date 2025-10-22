using Microsoft.EntityFrameworkCore;
using SpaceTraders.Models;
using SpaceTraders.Services.EfCache;
using SpaceTraders.Services.Surveys.Interfaces;

namespace SpaceTraders.Services.Surveys;

public class SurveysEfCacheServices(SpaceTradersDbContext _context) : ISurveysCacheService
{
    public Task DeleteAsync(string signature)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Survey>> GetAsync()
    {
        return await _context.Surveys.ToListAsync();
    }

    public async Task<IEnumerable<Survey>> GetAsync(string waypointSymbol)
    {
        return await _context
            .Surveys
            .Where(s => s.Symbol == waypointSymbol && s.Expiration > DateTime.UtcNow)
            .ToListAsync();
    }

    public async Task SetAsync(Survey survey)
    {
        _context.Surveys.Update(survey);
        await _context.SaveChangesAsync();
    }
}