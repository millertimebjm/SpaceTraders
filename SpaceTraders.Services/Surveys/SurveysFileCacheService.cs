using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.EfCache;
using SpaceTraders.Services.IoWrappers.Interfaces;
using SpaceTraders.Services.Surveys.Interfaces;

namespace SpaceTraders.Services.Surveys;

public class SurveysFileCacheServices(
    IFileWrapper _fileWrapper,
    IConfiguration _config) : ISurveysCacheService
{
    private bool _isLoaded = false;
    private string _filename { get { return $"{this.GetType()}_{_config[ConfigurationEnums.AgentToken.ToString()]}.txt"; } }

    private readonly Dictionary<string, Survey> _surveys = new ();

    private async Task CheckIsLoaded()
    {
        if (_isLoaded) return;
        if (!_fileWrapper.Exists(_filename)) return;
        
        var lines = await _fileWrapper.ReadAllLinesAsync(_filename);
        foreach (var line in lines)
        {
            var survey = JsonSerializer.Deserialize<Survey>(line);
            _surveys[survey.Symbol] = survey;
        }
        _isLoaded = true;
    }

    public async Task DeleteAsync(string signature)
    {
        await CheckIsLoaded();
        var survey = await GetBySignatureAsync(signature);
        _surveys.Remove(survey.Symbol);
        await SaveChangesAsync();
    }

    private async Task<Survey?> GetBySignatureAsync(string signature)
    {
        await CheckIsLoaded();
        return _surveys.Values.SingleOrDefault(s => s.Signature == signature && s.Expiration > DateTime.UtcNow);
    }
    
    private async Task SaveChangesAsync()
    {
        _ = FileSaveAsync();
    }

    public async Task<IEnumerable<Survey>> GetAsync()
    {
        await CheckIsLoaded();
        return _surveys.Values;
    }

    public async Task<IEnumerable<Survey>> GetAsync(string waypointSymbol)
    {
        await CheckIsLoaded();
        return _surveys
            .Values
            .Where(s => s.Symbol == waypointSymbol && s.Expiration > DateTime.UtcNow);
    }

    public async Task SetAsync(Survey survey)
    {
        await CheckIsLoaded();
        _surveys[survey.Symbol] = survey;
        await SaveChangesAsync();
    }

    private async Task FileSaveAsync()
    {
        var surveys = _surveys.Values;
        await _fileWrapper.WriteAllLinesAsync(_filename, surveys.Select(s => JsonSerializer.Serialize(s)));
    }
}