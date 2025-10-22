using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;

namespace SpaceTraders.Services.Transactions;

public class TransactionsMongoCacheService(IMongoCollectionFactory _collectionFactory) : ITransactionsCacheService
{
    public async Task<IReadOnlyList<MarketTransaction>> GetAsync(string shipSymbol, int take = 200)
    {
        var filter = Builders<MarketTransaction>
            .Filter
            .Eq(w => w.ShipSymbol, shipSymbol);

        var collection = _collectionFactory.GetCollection<MarketTransaction>();
        var projection = Builders<MarketTransaction>.Projection.Exclude("_id");

        return await collection
            .Find(filter)
            .SortByDescending(w => w.Timestamp)
            .Limit(take)
            .Project<MarketTransaction>(projection)
            .ToListAsync();
    }

    public async Task SetAsync(MarketTransaction transaction)
    {
        var collection = _collectionFactory.GetCollection<MarketTransaction>();
        await collection.InsertOneAsync(transaction);
    }
}