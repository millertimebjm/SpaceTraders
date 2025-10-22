using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.IoWrappers.Interfaces;

namespace SpaceTraders.Services.Agents;

public class AgentFileCacheService(
    IFileWrapper _fileWrapper,
    IConfiguration _config) : IAgentFileCacheService
{
    private bool _isLoaded = false;
    private string _filename { get { return $"{this.GetType()}_{_config[ConfigurationEnums.AgentToken.ToString()]}.txt"; } }
    private Agent? _agent = null;

    public async Task<Agent> GetAsync()
    {
        await CheckIsLoaded();
        return _agent;
    }

    public async Task<Agent> SetAsync(Agent agent)
    {
        _agent = agent;
        await SaveChangesAsync();
        return _agent;
    }

    private async Task SaveChangesAsync()
    {
        _ = FileSaveAsync();
    }

    private async Task FileSaveAsync()
    {
        await _fileWrapper.WriteAllLinesAsync(_filename, [JsonSerializer.Serialize(_agent)]);
    }
    
    private async Task CheckIsLoaded()
    {
        if (_isLoaded) return;
        if (!_fileWrapper.Exists(_filename)) return;

        var lines = await _fileWrapper.ReadAllLinesAsync(_filename);
        if (lines.Count() > 0)
        {
            _agent = JsonSerializer.Deserialize<Agent>(lines[0]);
        }
        _isLoaded = true;
    }
}