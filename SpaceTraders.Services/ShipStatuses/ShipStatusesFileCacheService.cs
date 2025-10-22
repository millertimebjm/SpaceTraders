using System.Text.Json;
using SpaceTraders.Models;
using SpaceTraders.Services.IoWrappers.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipStatuses;

public class ShipStatusesFileCacheService(IFileWrapper _fileWrapper) : IShipStatusesCacheService
{
    private bool _isLoaded = false;
    private string _filename { get { return $"{this.GetType()}.txt"; } }
    private readonly Dictionary<string, ShipStatus> _shipStatuses = new ();

    public async Task<IEnumerable<ShipStatus>> GetAsync()
    {
        await CheckIsLoaded();
        return _shipStatuses.Values;
    }

    public async Task<ShipStatus> GetAsync(string shipSymbol)
    {
        await CheckIsLoaded();
        return _shipStatuses.GetValueOrDefault(shipSymbol);
    }

    public async Task SetAsync(ShipStatus shipStatus)
    {
        _shipStatuses[shipStatus.Ship.Symbol] = shipStatus;
        await SaveChangesAsync();
    }

    public async Task DeleteAsync()
    {
        _shipStatuses.Clear();
        await SaveChangesAsync();
    }

    private async Task SaveChangesAsync()
    {
        _ = FileSaveAsync();
    }

    private async Task FileSaveAsync()
    {
        var shipStatuses = _shipStatuses.Values;
        await _fileWrapper.WriteAllLinesAsync(_filename, shipStatuses.Select(s => JsonSerializer.Serialize(s)));
    }
    
    private async Task CheckIsLoaded()
    {
        if (_isLoaded) return;
        
        var lines = await _fileWrapper.ReadAllLinesAsync(_filename);
        foreach (var line in lines)
        {
            var shipStatus = JsonSerializer.Deserialize<ShipStatus>(line);
            _shipStatuses[shipStatus.Ship.Symbol] = shipStatus;
        }
        _isLoaded = true;
    }
}