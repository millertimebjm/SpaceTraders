using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.EntityFrameworkCache;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;

namespace SpaceTraders.Services.Transactions;

public class TransactionsCacheEfServices(SpaceTraderDbContext _context) : ITransactionsCacheService
{
    public async Task<IReadOnlyList<MarketTransaction>> GetAsync(string shipSymbol, int take = 200)
    {
        return (await _context
            .Transactions
            .Where(t => t.ShipSymbol == shipSymbol)
            .OrderByDescending(t => t.Timestamp)
            .Take(take)
            .ToListAsync())
            .Select(t => JsonSerializer.Deserialize<MarketTransaction>(t.TransactionJson))
            .ToList();
        // var filter = Builders<MarketTransaction>
        //     .Filter
        //     .Eq(w => w.ShipSymbol, shipSymbol);

        // var collection = _collectionFactory.GetCollection<MarketTransaction>();
        // var projection = Builders<MarketTransaction>.Projection.Exclude("_id");

        // return await collection
        //     .Find(filter)
        //     .SortByDescending(w => w.Timestamp)
        //     .Limit(take)
        //     .Project<MarketTransaction>(projection)
        //     .ToListAsync();
    }

    public async Task SetAsync(MarketTransaction transaction)
    {
        await _context.AddAsync(new TransactionCacheModel(0, transaction.ShipSymbol, transaction.Timestamp, JsonSerializer.Serialize(transaction)));
        await _context.SaveChangesAsync();
        // var collection = _collectionFactory.GetCollection<MarketTransaction>();
        // await collection.InsertOneAsync(transaction);
    }
}