using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class HaulerShipJobService(
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    IContractsService _contractsService
) : IShipJobService
{
    public async Task<ShipCommand> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        if (!ships.Any(s => s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.FulfillContract))
        {
            var inventory = ship.Cargo.Inventory;
            if (inventory.Count == 0)
            {
                return new ShipCommand(ship.Symbol, ShipCommandEnum.FulfillContract);
            }

            var contract = await _contractsService.GetActiveAsync();
            if (contract is not null 
                && inventory.Count == 1 
                && inventory.Single().Symbol == contract.Terms.Deliver[0].TradeSymbol
                && inventory.Single().Units == contract.Terms.Deliver[0].UnitsRequired)
            {
                return new ShipCommand(ship.Symbol, ShipCommandEnum.FulfillContract);
            }
        }
        if (await IsSupplyConstruction(ships, ship))
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.SupplyConstruction);
        }
        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }

    public async Task<bool> IsSupplyConstruction(IEnumerable<Ship> ships, Ship ship)
    {
        var agent = await _agentsService.GetAsync();
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var unfinishedJumpGateWaypoint = system.Waypoints.SingleOrDefault(w => w.JumpGate is not null && w.IsUnderConstruction);
        
        if (!ships.Any(s => s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.SupplyConstruction)
            && agent.Credits > 800_000)
        {
            var inventory = ship.Cargo.Inventory;
            if (inventory.Count == 0)
            {
                return true;
            }

            if (ship.Cargo.Inventory.All(i => unfinishedJumpGateWaypoint.Construction.Materials.Where(m => m.Fulfilled < m.Required).Any(m => i.Symbol == m.TradeSymbol)))
            {
                return true;
            }
        }
        return false;
    }
}