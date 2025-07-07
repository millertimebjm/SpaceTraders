using SpaceTraders.Models;

namespace SpaceTraders.Services.Ships.Interfaces;

public interface IShipsService
{
    Task<IEnumerable<Ship>> GetAsync();
    Task<Ship> GetAsync(string shipSymbol);
    Task<Nav> OrbitAsync(string shipSymbol);
    Task<Nav> DockAsync(string shipSymbol);
    Task<(Nav, Fuel)> NavigateAsync(string waypointSymbol, string shipSymbol);
    Task<ExtractionResult> ExtractAsync(string shipSymbol);
    Task<ExtractionResult> ExtractAsync(string shipSymbol, Survey survey);
    Task<Nav> JumpAsync(string waypointSymbol, string shipSymbol);
    Task JettisonAsync(string shipSymbol, string inventorySymbol, int units);
    Task<SurveyResult> SurveyAsync(string shipSymbol);
}