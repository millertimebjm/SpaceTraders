using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.Transactions.Interfaces;

namespace SpaceTraders.Services.Transactions;

public class TransactionsServices : ITransactionsService
{
    public readonly IMongoCollectionFactory _collectionFactory;
    public TransactionsServices(IMongoCollectionFactory collectionFactory)
    {
        _collectionFactory = collectionFactory;
    }

    public async Task<IReadOnlyList<MarketTransaction>> GetAsync(string shipSymbol)
    {
        var filter = Builders<MarketTransaction>
            .Filter
            .Eq(w => w.ShipSymbol, shipSymbol);

        var collection = _collectionFactory.GetCollection<MarketTransaction>();
        var projection = Builders<MarketTransaction>.Projection.Exclude("_id");

        return await collection
            .Find(filter)
            .SortByDescending(w => w.Timestamp)
            .Limit(40)
            .Project<MarketTransaction>(projection)
            .ToListAsync();
    }

    public async Task SetAsync(MarketTransaction transaction)
    {
        var collection = _collectionFactory.GetCollection<MarketTransaction>();
        await collection.InsertOneAsync(transaction);
    }
}