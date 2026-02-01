using MongoDB.Driver;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Trades.Interfaces;

namespace SpaceTraders.Services.Trades;

public class TradesCacheMongoService(IMongoCollectionFactory _collectionFactory) : ITradesCacheService
{
     public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync()
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        var projection = Builders<TradeModel>.Projection.Exclude("_id");

        return await collection
            .Find(FilterDefinition<TradeModel>.Empty)
            .Project<TradeModel>(projection)
            .ToListAsync();
    }

    public async Task SaveTradeModelsAsync(IReadOnlyList<TradeModel> tradeModels)
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        await collection.DeleteManyAsync(FilterDefinition<TradeModel>.Empty);
        await collection.InsertManyAsync(tradeModels, new InsertManyOptions(), CancellationToken.None);

    }
}