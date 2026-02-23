using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;

namespace SpaceTraders.Services.ShipLogs;

public class ShipLogsStorageMongoService(IMongoCollectionFactory _collectionFactory) : IShipLogsStorageService
{
    private const int TAKE_DEFAULT = 100;
    private const int SKIP_DEFAULT = 0;

    public async Task<IEnumerable<ShipLog>> GetAsync(ShipLogsFilterModel model)
    {
        var filter = Builders<ShipLog>.Filter.Empty;

        var shipSymbolFilter = filter;
        if (!string.IsNullOrWhiteSpace(model.ShipSymbol))
        {
            shipSymbolFilter = Builders<ShipLog>.Filter.Eq(s => s.ShipSymbol, model.ShipSymbol);
        }

        var shipLogEnumFilter = filter;
        if (model.ShipLogEnum != null)
        {
            shipLogEnumFilter = Builders<ShipLog>.Filter.Eq(s => s.ShipLogEnum, model.ShipLogEnum);
        }

        var sort = Builders<ShipLog>.Sort.Descending(s => s.StartedDateTimeUtc);
        // if (model.ShipLogsFilterSortModel == ShipLogsFilterSortModel.DateDescending)
        // {
        //     shipLogSortFilter = Builders<ShipLog>.Sort.Descending("StartedDateTimeUtc");
        // }

        filter = shipSymbolFilter & shipLogEnumFilter;

        int take = Math.Clamp(model.Take ?? TAKE_DEFAULT, 1, 200);
        int skip = Math.Max(model.Skip ?? SKIP_DEFAULT, 0);
        
        var collection = _collectionFactory.GetCollection<ShipLog>();
        var projection = Builders<ShipLog>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Sort(sort)
            .Skip(skip)
            .Limit(take)
            .Project<ShipLog>(projection)
            .ToListAsync();
    }

    public async Task SetAsync(ShipLog shipLog)
    {
        var collection = _collectionFactory.GetCollection<ShipLog>();
        await collection.InsertOneAsync(shipLog);
    }

    public void Set(ShipLog shipLog)
    {
        var collection = _collectionFactory.GetCollection<ShipLog>();
        collection.InsertOne(shipLog);
    }

    public async Task SetAsync(IEnumerable<ShipLog> shipLogs)
    {
        var collection = _collectionFactory.GetCollection<ShipLog>();
        await collection.InsertManyAsync(shipLogs);
    }
}