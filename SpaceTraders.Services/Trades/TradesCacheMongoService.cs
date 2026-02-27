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

    public async Task<IEnumerable<TradeModel>?> GetTradeModelsAsync(int fuelMax, int fuelCurrent)
    {
        string key = $"{fuelMax}-{fuelCurrent}";
        var collection = _collectionFactory.GetCollection<TradeCacheModel>();
        var projection = Builders<TradeCacheModel>.Projection.Exclude("_id");

        var filter = Builders<TradeCacheModel>.Filter.Eq(tcm => tcm.Key, key);

        var tradeCacheModel = await collection
            .Find(filter)
            .Project<TradeCacheModel>(projection)
            .SingleOrDefaultAsync();
        return tradeCacheModel?.TradeModels;
    }

    public async Task SaveTradeModelsAsync(IEnumerable<TradeModel> tradeModels, int fuelMax, int fuelCurrent)
    {
        string key = $"{fuelMax}-{fuelCurrent}";
        var collection = _collectionFactory.GetCollection<TradeCacheModel>();
        var filter = Builders<TradeCacheModel>.Filter.Eq(tcm => tcm.Key, key);

        await collection.DeleteOneAsync(filter, CancellationToken.None);
        await collection.InsertOneAsync(new TradeCacheModel(key, tradeModels), new InsertOneOptions(), CancellationToken.None);
    }
}

public record TradeCacheModel(string Key, IEnumerable<TradeModel> TradeModels);