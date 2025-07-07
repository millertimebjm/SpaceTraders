using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.Surveys.Interfaces;

namespace SpaceTraders.Services.Surveys;

public class SurveysCacheService : ISurveysCacheService
{
    private readonly IMongoCollectionFactory _collectionFactory;
    public SurveysCacheService(IMongoCollectionFactory collectionFactory)
    {
        _collectionFactory = collectionFactory;
    }

    public async Task<IEnumerable<Survey>> GetAsync()
    {
        var collection = _collectionFactory.GetCollection<Survey>();

        // var filter = Builders<Survey>
        //     .Filter
        //     .Eq(s => s.Symbol, waypoint.SystemSymbol);
        var projection = Builders<Survey>.Projection.Exclude("_id");
        var surveys = await collection
            .Find(FilterDefinition<Survey>.Empty)
            .Project<Survey>(projection)
            .ToListAsync();
        return surveys;
    }

    public async Task<IEnumerable<Survey>> GetAsync(string waypointSymbol)
    {
        var collection = _collectionFactory.GetCollection<Survey>();

        var filter = Builders<Survey>
            .Filter
            .And(
                Builders<Survey>.Filter.Eq(s => s.Symbol, waypointSymbol),
                Builders<Survey>.Filter.Gt(s => s.Expiration, DateTime.UtcNow)
            );
            
        var projection = Builders<Survey>.Projection.Exclude("_id");
        var surveys = await collection
            .Find(filter)
            .Project<Survey>(projection)
            .ToListAsync();
        return surveys;
    }

    public async Task SetAsync(Survey survey)
    {
        var collection = _collectionFactory.GetCollection<Survey>();

        var filter = Builders<Survey>
            .Filter
            .Eq(s => s.Signature, survey.Signature);
        var projection = Builders<Survey>.Projection.Exclude("_id");
        await collection.DeleteOneAsync(filter, CancellationToken.None);
        await collection.InsertOneAsync(survey, new InsertOneOptions() { }, CancellationToken.None);
    }
}