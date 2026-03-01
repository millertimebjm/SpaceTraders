using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.Accounts.Interfaces;
using SpaceTraders.Services.MongoCache.Interfaces;

namespace SpaceTraders.Services.Accounts;

public class AccountCacheMongoService(IMongoCollectionFactory _collectionFactory) : IAccountCacheService
{
    public async Task<Account> GetAsync()
    {
        var collection = _collectionFactory.GetCollection<Account>();
        var projection = Builders<Account>.Projection.Exclude("_id");
        return await collection
            .Find(FilterDefinition<Account>.Empty)
            .Project<Account>(projection)
            .SingleOrDefaultAsync();
    }

    public async Task SetAsync(Account account)
    {
        var collection = _collectionFactory.GetCollection<Account>();
        await collection.DeleteManyAsync(FilterDefinition<Account>.Empty, CancellationToken.None);
        await collection.InsertOneAsync(account);
    }
}