using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.ServerStatusServices.Interfaces;

namespace SpaceTraders.Services.ServerStatusServices;

public class ServerStatusCacheMongoService(IMongoCollectionFactory _collectionFactory) : IServerStatusCacheService
{
    public async Task<ServerStatus?> GetAsync()
    {
        var collection = _collectionFactory.GetCollection<ServerStatus>();
        var projection = Builders<ServerStatus>.Projection.Exclude("_id");
        return await collection
            .Find(FilterDefinition<ServerStatus>.Empty)
            .Project<ServerStatus>(projection)
            .SingleOrDefaultAsync();
    }

    public async Task SetAsync(ServerStatus serverStatus)
    {
        var collection = _collectionFactory.GetCollection<ServerStatus>();
        await collection.DeleteManyAsync(FilterDefinition<ServerStatus>.Empty, CancellationToken.None);
        await collection.InsertOneAsync(serverStatus);
    }
}