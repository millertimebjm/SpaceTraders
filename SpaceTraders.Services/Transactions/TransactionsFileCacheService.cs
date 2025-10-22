using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.IoWrappers.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;

namespace SpaceTraders.Services.Transactions;

public class TransactionsFileCacheService(
    IFileWrapper _fileWrapper,
    IConfiguration _config
) : ITransactionsCacheService
{
    private bool _isLoaded = false;
    private string _filename { get { return $"{this.GetType()}_{_config[ConfigurationEnums.AgentToken.ToString()]}.txt"; } }
    private readonly Dictionary<DateTime, MarketTransaction> _transactions = new();
    public async Task<IReadOnlyList<MarketTransaction>> GetAsync(string shipSymbol, int take = 200)
    {
        await CheckIsLoaded();
        return _transactions.OrderBy(t => t.Key).Select(t => t.Value).Take(take).ToList();
    }

    // public async Task<IReadOnlyList<MarketTransaction>> GetAsync(string shipSymbol, int take = 200)
    // {
    //     var filter = Builders<MarketTransaction>
    //         .Filter
    //         .Eq(w => w.ShipSymbol, shipSymbol);

    //     var collection = _collectionFactory.GetCollection<MarketTransaction>();
    //     var projection = Builders<MarketTransaction>.Projection.Exclude("_id");

    //     return await collection
    //         .Find(filter)
    //         .SortByDescending(w => w.Timestamp)
    //         .Limit(take)
    //         .Project<MarketTransaction>(projection)
    //         .ToListAsync();
    // }

    public async Task SetAsync(MarketTransaction transaction)
    {
        _transactions[transaction.Timestamp] = transaction;
        await FileSaveAsync();
    }
    // public async Task SetAsync(MarketTransaction transaction)
    // {
    //     var collection = _collectionFactory.GetCollection<MarketTransaction>();
    //     await collection.InsertOneAsync(transaction);
    // }

    private async Task CheckIsLoaded()
    {
        if (_isLoaded) return;
        if (!_fileWrapper.Exists(_filename)) return;

        var lines = await _fileWrapper.ReadAllLinesAsync(_filename);
        foreach (var line in lines)
        {
            var transaction = JsonSerializer.Deserialize<MarketTransaction>(line);
            if (transaction != null)
            {
                _transactions[transaction.Timestamp] = transaction;
            }
        }
        _isLoaded = true;
    }

    private async Task FileSaveAsync()
    {
        var waypoints = _transactions.Values.ToList();
        await _fileWrapper.WriteAllLinesAsync(_filename, waypoints.Select(w => JsonSerializer.Serialize(w)));
    }
}