using SpaceTraders.Models;
using SpaceTraders.Models.Enums;

namespace SpaceTraders.Services.Ships.Interfaces;

public interface IShipsService
{
    Task<IEnumerable<Ship>> GetAsync();
    Task<Ship> GetAsync(string shipSymbol);
    Task<Nav> OrbitAsync(string shipSymbol);
    Task<Nav> DockAsync(string shipSymbol);
    Task<(Nav, Fuel)> NavigateAsync(Waypoint waypoint, Waypoint currentWaypoint, Ship ship);
    Task<(Nav, Fuel)> NavigateAsync(string waypointSymbol, Ship ship);
    Task<ExtractionResult> ExtractAsync(string shipSymbol);
    Task<ExtractionResult> ExtractAsync(string shipSymbol, Survey survey);
    Task<(Nav, Cooldown)> JumpAsync(string waypointSymbol, string shipSymbol);
    Task JettisonAsync(string shipSymbol, string inventorySymbol, int units);
    Task<SurveyResult> SurveyAsync(string shipSymbol);
    Task<ScanWaypointsResult> ScanWaypointsAsync(string shipSymbol);
    Task NavToggleAsync(string shipSymbol, string flightMode);
    Task<ChartWaypointResult> ChartAsync(string waypointSymbol);
    Task<ScanSystemsResult> ScanSystemsAsync(string shipSymbol);
    Task SwitchShipFlightMode(Ship ship, NavFlightModeEnum flightMode);
}