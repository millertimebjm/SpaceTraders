using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.MongoCache.Interfaces;

namespace SpaceTraders.Services.Contracts;

public class ContractsCacheMongoService(
    IMongoCollectionFactory _collectionFactory
) : IContractsCacheService
{
    public async Task<IEnumerable<STContract>> GetAsync()
    {
        var collection = _collectionFactory.GetCollection<STContract>();
        var projection = Builders<STContract>.Projection.Exclude("_id");

        var contracts = await collection
            .Find(FilterDefinition<STContract>.Empty)
            .Project<STContract>(projection)
            .ToListAsync();
        
        return contracts;
    }

    public async Task<STContract> GetAsync(string id)
    {
        var filter = Builders<STContract>
            .Filter
            .Eq(w => w.Id, id);
        var collection = _collectionFactory.GetCollection<STContract>();
        var projection = Builders<STContract>.Projection.Exclude("_id");
        var contracts = await collection
            .Find(filter)
            .Project<STContract>(projection)
            .SingleAsync();
        
        return contracts;
    }

    public async Task SetAsync(STContract contract)
    {
        var filter = Builders<STContract>.Filter.Eq(c => c.Id, contract.Id);
        var collection = _collectionFactory.GetCollection<STContract>();
        var projection = Builders<STContract>.Projection.Exclude("_id");

        var contracts = await collection
            .Find(filter)
            .Project<STContract>(projection)
            .SingleAsync();

        await collection.DeleteOneAsync(filter, CancellationToken.None);
        await collection.InsertOneAsync(contract, options: null, CancellationToken.None);
    }

    public async Task SetAsync(IEnumerable<STContract> contracts)
    {
        var collection = _collectionFactory.GetCollection<STContract>();
        await collection.DeleteManyAsync(FilterDefinition<STContract>.Empty, CancellationToken.None);
        await collection.InsertManyAsync(contracts, options: null, CancellationToken.None);
    }
}