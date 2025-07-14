using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class CommandShipJobService : IShipJobService
{
    private readonly IAgentsService _agentsService;
    private readonly ISystemsService _systemsService;
    public CommandShipJobService(
        IAgentsService agentsService,
        ISystemsService systemsService)
    {
        _agentsService = agentsService;
        _systemsService = systemsService;
    }

    public async Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var agent = await _agentsService.GetAsync();
        if (agent.Credits > 800_000)
        {
            var shipTypesInSystem = ships
                .Where(s => s.Nav.SystemSymbol == ship.Nav.SystemSymbol)
                .GroupBy(s => s.Registration.Role);
            var miningDrones = (shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.EXCAVATOR.ToString())?.Count() ?? 0);
            var lightHaulers = (shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.HAULER.ToString())?.Count() ?? 0);
            var surveyShips = (shipTypesInSystem.SingleOrDefault(st => st.Key == ShipRegistrationRolesEnum.SURVEYOR.ToString())?.Count() ?? 0);
            if (miningDrones < 5
                || lightHaulers < 5
                || miningDrones == 0)
            {
                return new ShipCommand(ship.Symbol, ShipCommandEnum.PurchaseShip);
            }
        }
        // var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        // var unchartedWaypoints =
        //     system.Waypoints.Where(w =>
        //         w.Traits is null
        //         || w.Traits.Any(t => t.Symbol == WaypointTraitsEnum.UNCHARTED.ToString()))
        //     .ToList();
        // if (unchartedWaypoints.Any())
        // {
        //     return new ShipCommand(ship.Symbol, ShipCommandEnum.Exploration);
        // }
        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }
}