using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Trades.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Waypoints;

public class WaypointsCacheMongoService(
    IMongoCollectionFactory _mongoCollectionFactory,
    ISystemsCacheService _systemsCacheService,
    ITradesService _tradesService
) : IWaypointsCacheService
{
    public async Task<Waypoint?> GetAsync(string waypointSymbol)
    {
        var filter = Builders<Waypoint>
            .Filter
            .Eq(w => w.Symbol, waypointSymbol);

        var collection = _mongoCollectionFactory.GetCollection<Waypoint>();

        var projection = Builders<Waypoint>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Project<Waypoint>(projection)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Waypoint>?> GetByTraitAsync(string systemSymbol, string trait)
    {
        var filter =
            Builders<Waypoint>.Filter.And(
                Builders<Waypoint>.Filter.Eq(w => w.SystemSymbol, systemSymbol),
                Builders<Waypoint>.Filter.ElemMatch(w => w.Traits, t => t.Symbol == trait)
            );
        var collection = _mongoCollectionFactory.GetCollection<Waypoint>();
        var projection = Builders<Waypoint>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Project<Waypoint>(projection)
            .ToListAsync();
    }

    public async Task<IEnumerable<Waypoint>?> GetByTypeAsync(string systemSymbol, string type)
    {
        var filter =
            Builders<Waypoint>.Filter.And(
                Builders<Waypoint>.Filter.Eq(w => w.SystemSymbol, systemSymbol),
                Builders<Waypoint>.Filter.Eq(w => w.Type, type)
            );
            
        var collection = _mongoCollectionFactory.GetCollection<Waypoint>();
        var projection = Builders<Waypoint>.Projection.Exclude("_id");
        return await collection
            .Find(filter)
            .Project<Waypoint>(projection)
            .ToListAsync();
    }

    public async Task SetAsync(Waypoint waypoint)
    {
        Task updateTradeModelTask = Task.CompletedTask;
        if (waypoint.Marketplace?.TradeGoods is not null
            && waypoint.Marketplace?.TradeGoods.Any(tg => tg.Symbol != TradeSymbolsEnum.FUEL.ToString() && tg.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString()) == true)
        {
            updateTradeModelTask = _tradesService.UpdateTradeModelAsync(waypoint.Symbol, waypoint.Marketplace.TradeGoods);
        }

        var filter = Builders<Waypoint>
            .Filter
            .Eq(w => w.Symbol, waypoint.Symbol);
        var waypointCollection = _mongoCollectionFactory.GetCollection<Waypoint>();
        await waypointCollection.ReplaceOneAsync(filter, waypoint, new ReplaceOptions { IsUpsert = true }, CancellationToken.None);
        await _systemsCacheService.SetAsync(waypoint);

        await updateTradeModelTask;
    }
}