using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpaceTraders.Models;
using SpaceTraders.Services.EntityFrameworkCache;
using SpaceTraders.Services.ShipStatuses.Interfaces;

namespace SpaceTraders.Services.ShipStatuses;

public class ShipStatusesCacheEfService(SpaceTraderDbContext _context) : IShipStatusesCacheService
{
    public async Task<IEnumerable<ShipStatus>> GetAsync()
    {
        var dbShipstatuses = await _context.ShipStatuses.ToListAsync();
        return dbShipstatuses.Select(ss => JsonSerializer.Deserialize<ShipStatus>(ss.ShipStatusJson));
        // var collection = _collectionFactory.GetCollection<ShipStatus>();
        // var projection = Builders<ShipStatus>.Projection.Exclude("_id");
        // return await collection
        //     .Find(FilterDefinition<ShipStatus>.Empty)
        //     .Project<ShipStatus>(projection)
        //     .ToListAsync();
    }

    public async Task<ShipStatus> GetAsync(string shipSymbol)
    {
        var shipStatusCacheModel = await _context.ShipStatuses.SingleAsync(ss => ss.Symbol == shipSymbol);
        return JsonSerializer.Deserialize<ShipStatus>(shipStatusCacheModel.ShipStatusJson);

        // var filter = Builders<ShipStatus>
        //     .Filter
        //     .Eq(s => s.Ship.Symbol, shipSymbol);
        // var collection = _collectionFactory.GetCollection<ShipStatus>();
        // var projection = Builders<ShipStatus>.Projection.Exclude("_id");
        // return await collection
        //     .Find(filter)
        //     .Project<ShipStatus>(projection)
        //     .SingleOrDefaultAsync();
    }

    public async Task SetAsync(ShipStatus shipStatus)
    {
        var dbShipStatus = await _context.ShipStatuses.SingleOrDefaultAsync(ss => ss.Symbol == shipStatus.Ship.Symbol);
        if (dbShipStatus != null)
        {
            var newDbShipStatus = dbShipStatus with { ShipStatusJson = JsonSerializer.Serialize(shipStatus) };
            _context.Entry(dbShipStatus).CurrentValues.SetValues(newDbShipStatus);
        }
        else
        {
            await _context.ShipStatuses.AddAsync(new ShipStatusCacheModel(shipStatus.Ship.Symbol, JsonSerializer.Serialize(shipStatus)));
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync()
    {
        await _context.ShipStatuses.ExecuteDeleteAsync();
    }

    public Task SetAsync(List<ShipStatus> shipStatuses)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(ShipStatus shipStatus)
    {
        throw new NotImplementedException();
    }
}