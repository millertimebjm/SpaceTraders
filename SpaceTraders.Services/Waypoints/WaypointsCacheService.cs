using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Waypoints;

public class WaypointsCacheService : IWaypointsCacheService
{
    private readonly ILogger<WaypointsCacheService> _logger;
    private readonly IMongoCollectionFactory _mongoCollectionFactory;

    public WaypointsCacheService(
        ILogger<WaypointsCacheService> logger,
        IMongoCollectionFactory mongoCollectionFactory)
    {
        _logger = logger;
        _mongoCollectionFactory = mongoCollectionFactory;
    }

    public async Task<Waypoint?> GetAsync(string waypointSymbol)
    {
        var filter = Builders<Waypoint>
            .Filter
            .Eq(w => w.Symbol, waypointSymbol);

        var collection = _mongoCollectionFactory.GetCollection<Waypoint>();

        var projection = Builders<Waypoint>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Project<Waypoint>(projection)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Waypoint>?> GetByTraitAsync(string waypointSymbol, string trait)
    {
        var filter = Builders<Waypoint>
            .Filter
            .ElemMatch(w => w.Traits, t => t.Symbol == trait);
        var collection = _mongoCollectionFactory.GetCollection<Waypoint>();
        var projection = Builders<Waypoint>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Project<Waypoint>(projection)
            .ToListAsync();
    }

    public async Task<IEnumerable<Waypoint>?> GetByTypeAsync(string waypointSymbol, string type)
    {
        var filter = Builders<Waypoint>
            .Filter
            .Eq(w => w.Type, type);
        var collection = _mongoCollectionFactory.GetCollection<Waypoint>();
        var projection = Builders<Waypoint>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Project<Waypoint>(projection)
            .ToListAsync();
    }

    public async Task SetAsync(Waypoint waypoint)
    {
        var filter = Builders<Waypoint>
            .Filter
            .Eq(w => w.Symbol, waypoint.Symbol);
        var collection = _mongoCollectionFactory.GetCollection<Waypoint>();
        await collection.DeleteOneAsync(filter, CancellationToken.None);
        await collection.InsertOneAsync(waypoint);
    }
}