using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpaceTraders.Models;
using SpaceTraders.Services.EntityFrameworkCache;
using SpaceTraders.Services.Surveys.Interfaces;

namespace SpaceTraders.Services.Surveys;

public class SurveysCacheEfService(SpaceTraderDbContext _context) : ISurveysCacheService
{
    public async Task<IEnumerable<Survey>> GetAsync()
    {
        var dbSurveys = await _context.Surveys.ToListAsync();
        return dbSurveys.Select(s => JsonSerializer.Deserialize<Survey>(s.SurveyJson));
        // var collection = _collectionFactory.GetCollection<Survey>();
        // var projection = Builders<Survey>.Projection.Exclude("_id");
        // var surveys = await collection
        //     .Find(FilterDefinition<Survey>.Empty)
        //     .Project<Survey>(projection)
        //     .ToListAsync();
        // return surveys;
    }

    public async Task<IEnumerable<Survey>> GetAsync(string waypointSymbol)
    {
        var dbSurveys = await _context.Surveys.Where(s => s.WaypointSymbol == waypointSymbol).ToListAsync();
        return dbSurveys.Select(s => JsonSerializer.Deserialize<Survey>(s.SurveyJson));

        // var collection = _collectionFactory.GetCollection<Survey>();

        // var filter = Builders<Survey>
        //     .Filter
        //     .And(
        //         Builders<Survey>.Filter.Eq(s => s.Symbol, waypointSymbol),
        //         Builders<Survey>.Filter.Gt(s => s.Expiration, DateTime.UtcNow)
        //     );

        // var projection = Builders<Survey>.Projection.Exclude("_id");
        // var surveys = await collection
        //     .Find(filter)
        //     .Project<Survey>(projection)
        //     .ToListAsync();
        // return surveys;
    }

    public async Task SetAsync(Survey survey)
    {
        var dbSurvey = await _context.Surveys.SingleOrDefaultAsync(s => s.Signature == survey.Signature);
        if (dbSurvey != null)
        {
            var newDbSurvey = dbSurvey with 
            { 
                SurveyJson = JsonSerializer.Serialize(survey)
            };
            _context.Entry(dbSurvey).CurrentValues.SetValues(newDbSurvey);
        }
        else
        {
            await _context.Surveys.AddAsync(new SurveyCacheModel(survey.Signature, survey.Symbol, JsonSerializer.Serialize(survey)));
        }
        await _context.SaveChangesAsync();
        // var collection = _collectionFactory.GetCollection<Survey>();

        // var filter = Builders<Survey>
        //     .Filter
        //     .Eq(s => s.Signature, survey.Signature);
        // var projection = Builders<Survey>.Projection.Exclude("_id");
        // await collection.DeleteOneAsync(filter, CancellationToken.None);
        // await collection.InsertOneAsync(survey, new InsertOneOptions() { }, CancellationToken.None);
    }

    public async Task DeleteAsync(string signature)
    {
        await _context.Surveys.Where(s => s.Signature == signature).ExecuteDeleteAsync();
        
        // var collection = _collectionFactory.GetCollection<Survey>();

        // var filter = Builders<Survey>
        //     .Filter
        //     .Eq(s => s.Signature, signature);
        // await collection.DeleteOneAsync(filter, CancellationToken.None);
    }
}