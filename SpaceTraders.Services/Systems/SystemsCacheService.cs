using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsCacheService : ISystemsCacheService
{
    private readonly IMongoCollectionFactory _collectionFactory;
    public SystemsCacheService(IMongoCollectionFactory collectionFactory)
    {
        _collectionFactory = collectionFactory;
    }

    public async Task<STSystem> GetAsync(string systemSymbol, bool refresh = false)
    {
        var filter = Builders<STSystem>
            .Filter
            .Eq(w => w.Symbol, systemSymbol);

        var collection = _collectionFactory.GetCollection<STSystem>();

        var projection = Builders<STSystem>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Project<STSystem>(projection)
            .FirstOrDefaultAsync();
    }

    public async Task SetAsync(STSystem system)
    {
        var filter = Builders<STSystem>
            .Filter
            .Eq(s => s.Symbol, system.Symbol);
        var collection = _collectionFactory.GetCollection<STSystem>();
        await collection.DeleteOneAsync(filter, CancellationToken.None);
        await collection.InsertOneAsync(system);
    }
}