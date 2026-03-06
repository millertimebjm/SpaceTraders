using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpaceTraders.Models;
using SpaceTraders.Services.EntityFrameworkCache;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsCacheEfService(SpaceTraderDbContext _context) : ISystemsCacheService
{
    public async Task<IReadOnlyList<STSystem>> GetAsync()
    {
        var systemCacheModels = await _context.STSystems.ToListAsync();
        return systemCacheModels.Select(s => JsonSerializer.Deserialize<STSystem>(s.STSystemJson)).ToList();
        // var collection = _collectionFactory.GetCollection<STSystem>();

        // var projection = Builders<STSystem>.Projection.Exclude("_id");
        // return await collection
        //     .Find(FilterDefinition<STSystem>.Empty)
        //     .Project<STSystem>(projection)
        //     .ToListAsync();
    }

    public async Task<STSystem?> GetAsync(string systemSymbol, bool refresh = false)
    {
        var systemCacheModels = await _context.STSystems.SingleOrDefaultAsync(s => s.Symbol == systemSymbol);
        if (systemCacheModels == null) return null;
        return JsonSerializer.Deserialize<STSystem>(systemCacheModels.STSystemJson);
        // var filter = Builders<STSystem>
        //     .Filter
        //     .Eq(w => w.Symbol, systemSymbol);

        // var collection = _collectionFactory.GetCollection<STSystem>();

        // var projection = Builders<STSystem>.Projection.Exclude("_id");
        // return await collection
        //     .Find(filter)
        //     .Project<STSystem>(projection)
        //     .FirstOrDefaultAsync();
    }

    public async Task SetAsync(STSystem system)
    {
        var dbSystem = await _context.STSystems.SingleOrDefaultAsync(s => s.Symbol == system.Symbol);
        if (dbSystem != null)
        {
            var newDbSystem = dbSystem with { STSystemJson = JsonSerializer.Serialize(system) };
            _context.Entry(dbSystem).CurrentValues.SetValues(newDbSystem);
        }
        else
        {
            await _context.STSystems.AddAsync(new STSystemCacheModel(system.Symbol, string.Join(",", system.Waypoints.Select(w => w.Symbol)), JsonSerializer.Serialize(system)));
        }
        await _context.SaveChangesAsync();

        // var filter = Builders<STSystem>
        //     .Filter
        //     .Eq(s => s.Symbol, system.Symbol);
        // var collection = _collectionFactory.GetCollection<STSystem>();
        // await collection.DeleteOneAsync(filter, CancellationToken.None);
        // await collection.InsertOneAsync(system);
    }
    public async Task SetAsync(Waypoint waypoint)
    {
        var dbSystemCacheModel = await _context.STSystems.SingleOrDefaultAsync(s => s.Waypoints.Contains(waypoint.Symbol));
        var dbSystem = JsonSerializer.Deserialize<STSystem>(dbSystemCacheModel.STSystemJson);
        var waypoints = dbSystem.Waypoints.Where(w => w.Symbol != waypoint.Symbol).ToList();
        waypoints.Add(waypoint);
        var newDbSystem = dbSystem with { Waypoints = waypoints };
        var newDbSystemCacheModel = dbSystemCacheModel with { STSystemJson = JsonSerializer.Serialize(newDbSystem) };
        _context.Entry(dbSystemCacheModel).CurrentValues.SetValues(newDbSystemCacheModel);
        await _context.SaveChangesAsync();
        // var filter = Builders<STSystem>
        //     .Filter
        //     .Eq(s => s.Symbol, waypoint.SystemSymbol);
        // var collection = _collectionFactory.GetCollection<STSystem>();
        // var projection = Builders<STSystem>.Projection.Exclude("_id");
        // var system = await collection
        //     .Find(filter)
        //     .Project<STSystem>(projection)
        //     .FirstOrDefaultAsync();

        // var waypoints = system.Waypoints.Where(w => w.Symbol != waypoint.Symbol).ToList();
        // waypoints.Add(waypoint);
        // system = system with { Waypoints = waypoints };

        // await collection.DeleteOneAsync(filter, CancellationToken.None);
        // await collection.InsertOneAsync(system);
    }
}