using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpaceTraders.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.EntityFrameworkCache;

namespace SpaceTraders.Services.Agents;

public class AgentsCacheEfService(SpaceTraderDbContext _context) : IAgentsCacheService
{
    public async Task<Agent?> GetAsync()
    {
        var dbAgent = await _context.Agents.FirstOrDefaultAsync();
        if ( dbAgent == null ) return null;
        return JsonSerializer.Deserialize<Agent>( dbAgent.AgentJson );
        // var collection = _mongoCollectionFactory.GetCollection<Agent>();
        // var projection = Builders<Agent>.Projection.Exclude("_id");
        // return await collection
        //     .Find(FilterDefinition<Agent>.Empty)
        //     .Project<Agent>(projection)
        //     .FirstOrDefaultAsync();
    }
        
    public async Task SetAsync(Agent agent)
    {
        var dbAgent = await _context.Agents.FirstOrDefaultAsync();
        if (dbAgent != null)
        {
            var newDbAgent = dbAgent with { AgentJson = JsonSerializer.Serialize(agent) };
            _context.Entry(dbAgent).CurrentValues.SetValues(newDbAgent);
        }
        else
        {
            await _context.Agents.AddAsync(new AgentCacheModel(agent.Symbol, JsonSerializer.Serialize(agent)));
        }
        await _context.SaveChangesAsync();
        // var collection = _mongoCollectionFactory.GetCollection<Agent>();
        // await collection.DeleteManyAsync(FilterDefinition<Agent>.Empty, CancellationToken.None);
        // await collection.InsertOneAsync(agent, new InsertOneOptions() { }, CancellationToken.None);
    }
}