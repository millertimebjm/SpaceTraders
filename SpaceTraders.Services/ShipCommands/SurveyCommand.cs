using SpaceTraders.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class SurveyCommand(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    IAgentsService _agentsService,
    ITransactionsCacheService _transactionsService
) : IShipCommandsService
{
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var ship = shipStatus.Ship;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        while (true)
        {
            if (ShipsService.GetShipCooldown(ship) is not null) return shipStatus;

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                await _transactionsService.SetAsync(refuelResponse.Transaction);
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForFuel(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
                continue;
            }
            
            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            (nav, var fuel) = await _shipCommandsHelperService.NavigateToSurvey(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                return new ShipStatus(ship, $"Navigate To Survey {nav.WaypointSymbol}", DateTime.UtcNow);
            }

            var cooldown = await _shipCommandsHelperService.Survey(ship);
            ship = ship with { Cooldown = cooldown };
            if (ship.Cooldown.TotalSeconds > 10)
            {
                return new ShipStatus(ship, $"Resetting job after surveying.", DateTime.UtcNow);
            }
            return new ShipStatus(ship, $"Survey {ship.Nav.WaypointSymbol}", DateTime.UtcNow);
        }
    }
}