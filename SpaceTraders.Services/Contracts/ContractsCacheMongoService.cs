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

    public async Task<STContract> GetAsync(string contractId)
    {
        var filter = Builders<STContract>
            .Filter
            .Eq(w => w.ContractId, contractId);
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
        var filter = Builders<STContract>.Filter.Eq(c => c.ContractId, contract.ContractId);
        var collection = _collectionFactory.GetCollection<STContract>();
        var projection = Builders<STContract>.Projection.Exclude("_id");

        await collection.ReplaceOneAsync(filter, contract, new ReplaceOptions { IsUpsert = true }, CancellationToken.None);
    }

    public async Task SetAsync(IEnumerable<STContract> contracts)
    {
        var collection = _collectionFactory.GetCollection<STContract>();
        foreach (var contract in contracts)
        {
            var filter = Builders<STContract>.Filter.Eq(c => c.ContractId, contract.ContractId);
            await collection.ReplaceOneAsync(filter, contract, new ReplaceOptions { IsUpsert = true }, CancellationToken.None);
        }
    }
}