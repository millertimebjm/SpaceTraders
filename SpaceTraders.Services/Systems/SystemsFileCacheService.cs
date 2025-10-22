using System.Text.Json;
using SpaceTraders.Models;
using SpaceTraders.Services.IoWrappers.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsFileCacheService(IFileWrapper _fileWrapper) : ISystemsCacheService
{
    private bool _isLoaded = false;
    private string _filename { get { return $"{this.GetType()}.txt"; } }
    private readonly Dictionary<string, STSystem> _systems = new ();

    public async Task<IReadOnlyList<STSystem>> GetAsync()
    {
        await CheckIsLoaded();
        return _systems.Values.ToList();
    }

    public async Task<STSystem> GetAsync(string systemSymbol, bool refresh = false)
    {
        await CheckIsLoaded();
        return _systems.GetValueOrDefault(systemSymbol);
    }

    public async Task SetAsync(STSystem system)
    {
        await CheckIsLoaded();
        _systems[system.Symbol] = system;
        await FileSaveAsync();
    }

    public async Task SetAsync(Waypoint waypoint)
    {
        await CheckIsLoaded();
        var system = _systems[waypoint.SystemSymbol];
        var waypoints = system.Waypoints.Where(w => w.Symbol != waypoint.Symbol).ToList();
        waypoints.Add(waypoint);
        _systems[system.Symbol] = system;
        await FileSaveAsync();
    }

    private async Task CheckIsLoaded()
    {
        if (_isLoaded) return;

        var lines = await _fileWrapper.ReadAllLinesAsync(_filename);
        foreach (var line in lines)
        {
            var system = JsonSerializer.Deserialize<STSystem>(line);
            if (system != null)
            {
                _systems[system.Symbol] = system;
            }
        }
        _isLoaded = true;
    }

    private async Task FileSaveAsync()
    {
        var waypoints = _systems.Values.ToList();
        await _fileWrapper.WriteAllLinesAsync(_filename, waypoints.Select(w => JsonSerializer.Serialize(w)));
    }
}