using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.MongoCache.Interfaces;

namespace SpaceTraders.Services.Agents;

public class AgentsCacheMongoService(
    IMongoCollectionFactory _mongoCollectionFactory
) : IAgentsCacheService
{
    public async Task<Agent?> GetAsync()
    {
        var collection = _mongoCollectionFactory.GetCollection<Agent>();
        var projection = Builders<Agent>.Projection.Exclude("_id");
        return await collection
            .Find(FilterDefinition<Agent>.Empty)
            .Project<Agent>(projection)
            .FirstOrDefaultAsync();
    }
        
    public async Task SetAsync(Agent agent)
    {
        var collection = _mongoCollectionFactory.GetCollection<Agent>();
        //await collection.DeleteManyAsync(FilterDefinition<Agent>.Empty, CancellationToken.None);
        //await collection.InsertOneAsync(agent, new InsertOneOptions() { }, CancellationToken.None);
        var filter = Builders<Agent>.Filter.Eq(a => a.Symbol, agent.Symbol);
        await collection.ReplaceOneAsync(filter, agent, new ReplaceOptions { IsUpsert = true }, CancellationToken.None);
    }
}