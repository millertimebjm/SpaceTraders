using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipStatuses;

public class ShipStatusesCacheService : IShipStatusesCacheService
{
    private readonly IMongoCollectionFactory _collectionFactory;
    public ShipStatusesCacheService(IMongoCollectionFactory collectionFactory)
    {
        _collectionFactory = collectionFactory;
    }

    public async Task<IEnumerable<ShipStatus>> GetAsync()
    {
        var collection = _collectionFactory.GetCollection<ShipStatus>();
        var projection = Builders<ShipStatus>.Projection.Exclude("_id");
        return await collection
            .Find(FilterDefinition<ShipStatus>.Empty)
            .Project<ShipStatus>(projection)
            .ToListAsync();
    }

    public async Task<ShipStatus> GetAsync(string shipSymbol)
    {
        var filter = Builders<ShipStatus>
            .Filter
            .Eq(s => s.Ship.Symbol, shipSymbol);
        var collection = _collectionFactory.GetCollection<ShipStatus>();
        var projection = Builders<ShipStatus>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Project<ShipStatus>(projection)
            .SingleOrDefaultAsync();
    }

    public async Task SetAsync(ShipStatus shipStatus)
    {
        var filter = Builders<ShipStatus>
            .Filter
            .Eq(s => s.Ship.Symbol, shipStatus.Ship.Symbol);
        var collection = _collectionFactory.GetCollection<ShipStatus>();
        await collection.DeleteOneAsync(filter, CancellationToken.None);
        await collection.InsertOneAsync(shipStatus);
    }

    public async Task DeleteAsync()
    {
        var collection = _collectionFactory.GetCollection<ShipStatus>();
        await collection.DeleteManyAsync(FilterDefinition<ShipStatus>.Empty, CancellationToken.None);
    }
}