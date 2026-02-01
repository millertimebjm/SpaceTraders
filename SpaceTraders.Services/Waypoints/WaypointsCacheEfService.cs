using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.EntityFrameworkCache;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Waypoints;

public class WaypointsCacheEfService(
    SpaceTraderDbContext _context,
    ISystemsCacheService _systemsCacheService
) : IWaypointsCacheService
{
    public async Task<Waypoint?> GetAsync(string waypointSymbol)
    {
        var dbWaypointCacheModel = await _context.Waypoints.SingleOrDefaultAsync(w => w.Symbol == waypointSymbol);
        if (dbWaypointCacheModel == null) return null;
        return JsonSerializer.Deserialize<Waypoint?>(dbWaypointCacheModel.WaypointJson);
        //return JsonSerializer.Deserialize<Waypoint?>(dbWaypointCacheModel.);
        // var filter = Builders<Waypoint>
        //     .Filter
        //     .Eq(w => w.Symbol, waypointSymbol);

        // var collection = _mongoCollectionFactory.GetCollection<Waypoint>();

        // var projection = Builders<Waypoint>.Projection.Exclude("_id");
        // return await collection
        //     .Find(filter)
        //     .Project<Waypoint>(projection)
        //     .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Waypoint>?> GetByTraitAsync(string systemSymbol, string trait)
    {
        var dbWaypointCacheModels = await _context.Waypoints.Where(w => w.SystemSymbol == systemSymbol && w.Traits.Contains($",{trait},")).ToListAsync();
        return dbWaypointCacheModels.Select(w => JsonSerializer.Deserialize<Waypoint>(w.WaypointJson));
        // var filter =
        //     Builders<Waypoint>.Filter.And(
        //         Builders<Waypoint>.Filter.Eq(w => w.SystemSymbol, systemSymbol),
        //         Builders<Waypoint>.Filter.ElemMatch(w => w.Traits, t => t.Symbol == trait)
        //     );
        // var collection = _mongoCollectionFactory.GetCollection<Waypoint>();
        // var projection = Builders<Waypoint>.Projection.Exclude("_id");
        // return await collection
        //     .Find(filter)
        //     .Project<Waypoint>(projection)
        //     .ToListAsync();
    }

    public async Task<IEnumerable<Waypoint>?> GetByTypeAsync(string systemSymbol, string type)
    {
        var dbWaypointCacheModels = await _context.Waypoints.Where(w => w.SystemSymbol == systemSymbol && w.Type == type).ToListAsync();
        return dbWaypointCacheModels.Select(w => JsonSerializer.Deserialize<Waypoint>(w.WaypointJson));
        // var filter =
        //     Builders<Waypoint>.Filter.And(
        //         Builders<Waypoint>.Filter.Eq(w => w.SystemSymbol, systemSymbol),
        //         Builders<Waypoint>.Filter.Eq(w => w.Type, type)
        //     );
            
        // var collection = _mongoCollectionFactory.GetCollection<Waypoint>();
        // var projection = Builders<Waypoint>.Projection.Exclude("_id");
        // return await collection
        //     .Find(filter)
        //     .Project<Waypoint>(projection)
        //     .ToListAsync();
    }

    public async Task SetAsync(Waypoint waypoint)
    {
        var dbWaypoint = await _context.Waypoints.SingleOrDefaultAsync(w => w.Symbol == waypoint.Symbol);
        if (dbWaypoint != null)
        {
            var newDbWaypoint = dbWaypoint with 
            { 
                WaypointJson = JsonSerializer.Serialize(waypoint),
                Type = waypoint.Type,
                Traits = "," + string.Join(",", waypoint.Traits) + ","
            };
            _context.Entry(dbWaypoint).CurrentValues.SetValues(newDbWaypoint);
        }
        if (dbWaypoint == null)
        {
            dbWaypoint = new WaypointCacheModel(
                Symbol: waypoint.Symbol,
                Type: waypoint.Type,
                Traits: "," + string.Join(",", waypoint.Traits) + ",",
                SystemSymbol: waypoint.SystemSymbol,
                WaypointJson: JsonSerializer.Serialize(waypoint)
            );
        }
        await _context.SaveChangesAsync();
        await _systemsCacheService.SetAsync(waypoint);
        // var filter = Builders<Waypoint>
        //     .Filter
        //     .Eq(w => w.Symbol, waypoint.Symbol);
        // var waypointCollection = _mongoCollectionFactory.GetCollection<Waypoint>();
        // await waypointCollection.DeleteOneAsync(filter, CancellationToken.None);
        // await waypointCollection.InsertOneAsync(waypoint);
        // await _systemsCacheService.SetAsync(waypoint);
    }
}