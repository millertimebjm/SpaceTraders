using System.Dynamic;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.IoWrappers.Interfaces;
using SpaceTraders.Services.Trades.Interfaces;

namespace SpaceTraders.Services.Trades;

public class TradeModelFileCacheService(
    IFileWrapper _fileWrapper,
    IConfiguration _config) : ITradeModelCacheService
{
    // public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync()
    // {
    //     var collection = _collectionFactory.GetCollection<TradeModel>();
    //     var projection = Builders<TradeModel>.Projection.Exclude("_id");

    //     return await collection
    //         .Find(FilterDefinition<TradeModel>.Empty)
    //         .Project<TradeModel>(projection)
    //         .ToListAsync();
    // }

    // public async Task SaveTradeModelsAsync(IReadOnlyList<Waypoint> waypoints, int fuelMax, int fuelCurrent)
    // {
    //     var tradeModels = await BuildTradeModel(waypoints, fuelMax, fuelCurrent);
    //     if (tradeModels.Any())
    //     {
    //         var collection = _collectionFactory.GetCollection<TradeModel>();
    //         await collection.DeleteManyAsync(FilterDefinition<TradeModel>.Empty);
    //         await collection.InsertManyAsync(tradeModels, new InsertManyOptions(), CancellationToken.None);
    //     }
    // }
    private bool _isLoaded = false;
    private List<TradeModelLog> _tradeModelLogs = new();
    private string _filename { get { return $"{this.GetType()}_{_config[ConfigurationEnums.AgentToken.ToString()]}.txt"; } }

    public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync()
    {
        await CheckIsLoaded();
        return (IReadOnlyList<TradeModel>)_tradeModelLogs
            .SingleOrDefault(tml => _tradeModelLogs.Select(tml => tml.DateTime).Max() == tml.DateTime)
            .Waypoints;
    }

    public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync(int fuelMax, int fuelCurrent)
    {
        await CheckIsLoaded();
        return (IReadOnlyList<TradeModel>)_tradeModelLogs
            .SingleOrDefault(tml => tml.FuelMax == fuelMax && tml.FuelCurrent == fuelCurrent)
            .Waypoints;
    }

    public async Task SaveTradeModelsAsync(IReadOnlyList<Waypoint> waypoints, int fuelMax, int fuelCurrent)
    {
        var tradeModelLog = _tradeModelLogs.SingleOrDefault(tml =>
            tml.Waypoints == waypoints
            && tml.FuelMax == fuelMax
            && tml.FuelCurrent == fuelCurrent);
        if (tradeModelLog is null) _tradeModelLogs.Add(new TradeModelLog(waypoints.ToList(), fuelMax, fuelCurrent, DateTime.UtcNow));
        await SaveChangesAsync();
    }

    private async Task SaveChangesAsync()
    {
        _ = FileSaveAsync();
    }

    private async Task FileSaveAsync()
    {
        await _fileWrapper.WriteAllLinesAsync(_filename, _tradeModelLogs.Select(s => JsonSerializer.Serialize(s)));
    }

    private async Task CheckIsLoaded()
    {
        if (_isLoaded) return;
        if (!_fileWrapper.Exists(_filename)) return;

        var lines = await _fileWrapper.ReadAllLinesAsync(_filename);
        foreach (var line in lines)
        {
            _tradeModelLogs.Add(JsonSerializer.Deserialize<TradeModelLog>(line));
        }
        _isLoaded = true;
    }
}

public record TradeModelLog(
    List<Waypoint> Waypoints,
    int FuelMax,
    int FuelCurrent,
    DateTime DateTime
);


