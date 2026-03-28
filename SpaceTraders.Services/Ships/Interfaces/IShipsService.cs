using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Models.Results;

namespace SpaceTraders.Services.Ships.Interfaces;

public interface IShipsService
{
    Task<IEnumerable<Ship>> GetAsync();
    Task<Ship> GetAsync(string shipSymbol);
    Task<Nav> OrbitAsync(string shipSymbol);
    Task<Nav> DockAsync(string shipSymbol);
    Task<(Nav, Fuel)> NavigateAsync(Waypoint waypoint, Waypoint currentWaypoint, Ship ship);
    Task<(Nav, Fuel)> NavigateAsync(string waypointSymbol, Ship ship);
    Task<SiphonResult> SiphonAsync(string shipSymbol);
    Task<ExtractionResult> ExtractAsync(string shipSymbol);
    Task<ExtractionResult> ExtractAsync(string shipSymbol, Survey survey);
    Task<(Nav, Cooldown)> JumpAsync(string waypointSymbol, string shipSymbol);
    Task<Cargo> JettisonAsync(string shipSymbol, string inventorySymbol, int units);
    Task<SurveyResult> SurveyAsync(string shipSymbol);
    Task<ScanWaypointsResult> ScanWaypointsAsync(string shipSymbol);
    Task<Nav> NavToggleAsync(Ship ship, NavFlightModeEnum flightMode);
    Task<ChartWaypointResult> ChartAsync(string waypointSymbol);
    Task<ScanSystemsResult> ScanSystemsAsync(string shipSymbol);
    Task<TransferCargoResult> TransferCargo(string shipSymbol, string targetShipSymbol, string inventorySymbol, int inventoryAmount);
    Task<ScrapShipResponse> ScrapShipAsync(string symbol);
    Task<InstallModuleResult> InstallModule(string shipSymbol, string symbol);
}