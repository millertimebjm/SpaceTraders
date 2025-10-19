using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class HaulerShipJobService(
    ISystemsService _systemsService,
    IAgentsService _agentsService
) : IShipJobService
{
    public async Task<ShipCommand> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var agent = await _agentsService.GetAsync();
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var unfinishedJumpGateWaypoint = system.Waypoints.SingleOrDefault(w => w.JumpGate is not null && w.IsUnderConstruction);
        var firstHauler = ships
            .Where(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString())
            .OrderBy(s => s.Symbol)
            .FirstOrDefault();
        if (unfinishedJumpGateWaypoint is not null
            && unfinishedJumpGateWaypoint.IsUnderConstruction
            && ships.Where(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()).Count() >= 5
            && ships.Where(s => s.Registration.Role == ShipRegistrationRolesEnum.EXCAVATOR.ToString()).Count() >= 9
            && !ships.Where(s => s.Symbol != ship.Symbol).Any(s => s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.SupplyConstruction)
            && ship.Symbol == firstHauler?.Symbol
            && (agent.Credits > 800_000
            || (ship.Cargo.Units > 0 && ship.Cargo.Inventory.All(i => unfinishedJumpGateWaypoint.Construction.Materials.Any(m => i.Symbol == m.TradeSymbol)))))
        {
            if (!ship.Cargo.Inventory.Any())
            {
                return new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.SupplyConstruction);
            }
            if (ship.Cargo.Inventory.All(i => unfinishedJumpGateWaypoint.Construction.Materials.Any(m => i.Symbol == m.TradeSymbol)))
            {
                return new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.SupplyConstruction);
            }
        }
        return new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.BuyToSell);
    }
}