using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class SurveyorShipJobService : IShipJobService
{
    public Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var shipsWithCommands = ships.Where(s => s.ShipCommand is not null);
        var miningShips = shipsWithCommands.Where(s => s.ShipCommand is not null && s.ShipCommand.ShipCommandEnum == Models.Enums.ShipCommandEnum.MiningToSellAnywhere);
        var miningWaypoints = miningShips.GroupBy(ms => ms.ShipCommand.StartWaypointSymbol);

        var surveyShips = shipsWithCommands.Where(s => s.Symbol != ship.Symbol && s.ShipCommand is not null && s.ShipCommand.ShipCommandEnum == Models.Enums.ShipCommandEnum.Survey);
        var surveyWaypoints = surveyShips.Select(ss => ss.ShipCommand.StartWaypointSymbol);

        var miningWaypointsNotSurveyed = miningWaypoints.Where(mw => !surveyWaypoints.Contains(mw.Key));
        var surveyWaypoint = miningWaypointsNotSurveyed.OrderByDescending(mw => mw.Count()).FirstOrDefault()?.Key;

        if (surveyWaypoint is not null)
        {
            return Task.FromResult(new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.Survey, surveyWaypoint));
        }
        return null;
    }
}