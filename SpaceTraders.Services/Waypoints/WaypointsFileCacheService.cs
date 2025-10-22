using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.IoWrappers.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Waypoints;

public class WaypointsFileCacheService(
    IFileWrapper _fileWrapper,
    IConfiguration _config) : IWaypointsCacheService
{
    private bool _isLoaded = false;
    private string _filename { get { return $"{this.GetType()}_{_config[ConfigurationEnums.AgentToken.ToString()]}.txt"; } }
    private readonly Dictionary<string, Waypoint> _waypoints = new ();

    public async Task<Waypoint?> GetAsync(string waypointSymbol)
    {
        await CheckIsLoaded();
        return _waypoints.GetValueOrDefault(waypointSymbol);
    }

    public async Task<IEnumerable<Waypoint>?> GetByTraitAsync(string systemSymbol, string trait)
    {
        await CheckIsLoaded();
        return _waypoints
            .Values
            .Where(w => w.SystemSymbol == systemSymbol && w.Traits.Any(t => t.Symbol == trait));
    }

    public async Task<IEnumerable<Waypoint>?> GetByTypeAsync(string systemSymbol, string type)
    {
        await CheckIsLoaded();
        return _waypoints
            .Values
            .Where(w => w.SystemSymbol == systemSymbol && w.Type == type);
    }

    public async Task SetAsync(Waypoint waypoint)
    {
        await CheckIsLoaded();
        _waypoints[waypoint.Symbol] = waypoint;
        await FileSaveAsync();
    }

    private async Task CheckIsLoaded()
    {
        if (_isLoaded) return;
        if (!_fileWrapper.Exists(_filename)) return;

        var lines = await _fileWrapper.ReadAllLinesAsync(_filename);
        foreach (var line in lines)
        {
            var waypoint = JsonSerializer.Deserialize<Waypoint>(line);
            if (waypoint != null)
            {
                _waypoints[waypoint.Symbol] = waypoint;
            }
        }
        _isLoaded = true;
    }

    private async Task FileSaveAsync()
    {
        var waypoints = _waypoints.Values.ToList();
        await _fileWrapper.WriteAllLinesAsync(_filename, waypoints.Select(w => JsonSerializer.Serialize(w)));
    }
}