using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;

namespace SpaceTraders.Services.ShipStatuses;

public class ShipStatusesCacheMongoService(IMongoCollectionFactory _collectionFactory) : IShipStatusesCacheService
{
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
        await collection.ReplaceOneAsync(filter, shipStatus, new ReplaceOptions { IsUpsert = true }, CancellationToken.None);
    }

    public async Task SetAsync(List<ShipStatus> shipStatuses)
    {
        var collection = _collectionFactory.GetCollection<ShipStatus>();
        foreach (var shipStatus in shipStatuses)
        {
            var filter = Builders<ShipStatus>.Filter.Eq(ss => ss.Ship.Symbol, shipStatus.Ship.Symbol);
            await collection.ReplaceOneAsync(filter, shipStatus, new ReplaceOptions { IsUpsert = true }, CancellationToken.None);
        }
        var removeFilter = Builders<ShipStatus>.Filter.Nin(s => s.Ship.Symbol, shipStatuses.Select(ss => ss.Ship.Symbol).ToList());
        await collection.DeleteManyAsync(removeFilter);
    }

    public async Task DeleteAsync(ShipStatus shipStatus)
    {
        var collection = _collectionFactory.GetCollection<ShipStatus>();
        var filter = Builders<ShipStatus>.Filter.Eq(ss => ss.Ship.Symbol, shipStatus.Ship.Symbol);
        await collection.DeleteOneAsync(filter, CancellationToken.None);
    }

}