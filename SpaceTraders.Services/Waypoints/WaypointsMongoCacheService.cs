using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Waypoints;

public class WaypointsMongoCacheService(
    IMongoCollectionFactory _mongoCollectionFactory,
    ISystemsCacheService _systemsCacheService
) : IWaypointsCacheService
{
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

    public async Task<IEnumerable<Waypoint>?> GetByTraitAsync(string systemSymbol, string trait)
    {
        var filter =
            Builders<Waypoint>.Filter.And(
                Builders<Waypoint>.Filter.Eq(w => w.SystemSymbol, systemSymbol),
                Builders<Waypoint>.Filter.ElemMatch(w => w.Traits, t => t.Symbol == trait)
            );
        var collection = _mongoCollectionFactory.GetCollection<Waypoint>();
        var projection = Builders<Waypoint>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Project<Waypoint>(projection)
            .ToListAsync();
    }

    public async Task<IEnumerable<Waypoint>?> GetByTypeAsync(string systemSymbol, string type)
    {
        var filter =
            Builders<Waypoint>.Filter.And(
                Builders<Waypoint>.Filter.Eq(w => w.SystemSymbol, systemSymbol),
                Builders<Waypoint>.Filter.Eq(w => w.Type, type)
            );
            
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
        var waypointCollection = _mongoCollectionFactory.GetCollection<Waypoint>();
        await waypointCollection.DeleteOneAsync(filter, CancellationToken.None);
        await waypointCollection.InsertOneAsync(waypoint);
        await _systemsCacheService.SetAsync(waypoint);
    }
}