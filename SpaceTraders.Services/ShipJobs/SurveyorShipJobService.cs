using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class SurveyorShipJobService : IShipJobService
{
    private readonly ISystemsService _systemsService;
    public SurveyorShipJobService(
        ISystemsService systemsService)
    {
        _systemsService = systemsService;
    }

    public Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        // var system = await _systemsService.GetAsync(ship.Nav.WaypointSymbol);
        // var currentWaypoint = system.Waypoints.Single(w => w.Symbol == ship.Nav.WaypointSymbol);

        // var shipsWithCommands = ships.Where(s => s.ShipCommand is not null);
        // var miningShips = shipsWithCommands.Where(s => s.ShipCommand is not null && s.ShipCommand.ShipCommandEnum == Models.Enums.ShipCommandEnum.MiningToSellAnywhere);
        // var miningWaypoints = system.Waypoints.Where(w => w.Type == WaypointTypesEnum.ASTEROID.ToString()
        //     || w.Type == WaypointTypesEnum.ENGINEERED_ASTEROID.ToString());
        // var paths = PathsService.BuildDijkstraPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);
        // var closestPath = paths
        //     .Where(p => miningWaypoints.Select(w => w.Symbol).Contains(p.Key.Symbol))
        //     .OrderBy(p => p.Value.Item1.Count())
        //     .SingleOrDefault();

        // if (closestPath is not null)
        // {
        //     return Task.FromResult(new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.Survey, surveyWaypoint));
        // }
        // return null;
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        return Task.FromResult(new ShipCommand(ship.Symbol, ShipCommandEnum.Survey));
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

    }
}