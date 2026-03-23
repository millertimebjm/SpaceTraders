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
        if (await IsContract(ships, ship))
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.FulfillContract);
        }
        if (await IsSupplyConstruction(ships, ship))
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.SupplyConstruction);
        }
        if (await IsHaulingAssist(ships, ship))
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.HaulingAssistToSellAnywhere);
        }
        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }

    public async Task<bool> IsHaulingAssist(IEnumerable<Ship> ships, Ship ship)
    {
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        if (system.Waypoints.Any(w => w.JumpGate is not null && !w.IsUnderConstruction)) return false;
        
        if (ships.Count(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()) > 3
            && !ships.Any(s => s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.HaulingAssistToSellAnywhere))
        {
            return true;
        }
        return false;
    }

    public async Task<bool> IsContract(IEnumerable<Ship> ships, Ship ship)
    {
        if (!ships.Any(s => s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.FulfillContract))
        {
            var contract = await _contractsService.GetActiveAsync();
            var contractInventory = contract.Terms.Deliver[0].TradeSymbol;
            var otherShips = ships.Where(s => s.Symbol != ship.Symbol).ToList();

            if (otherShips.Any(s => s.Cargo.Inventory.Any(i => i.Symbol == contractInventory) && s.Cargo.Inventory.Count() == 1))
            {
                return false;
            }
            
            var inventory = ship.Cargo.Inventory;
            if (inventory.Count == 0)
            {
                return true;
            }

            if (contract is not null 
                && inventory.Count == 1 
                && inventory.Single().Symbol == contractInventory)
            {
                return true;
            }
        }
        return false;
    }

    public async Task<bool> IsSupplyConstruction(IEnumerable<Ship> ships, Ship ship)
    {
        var agent = await _agentsService.GetAsync();
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var unfinishedJumpGateWaypoint = system.Waypoints.SingleOrDefault(w => w.JumpGate is not null && w.IsUnderConstruction);
        if (unfinishedJumpGateWaypoint is null) return false;
        
        if (ship.Cargo.Inventory.Any() && ship.Cargo.Inventory.All(i => unfinishedJumpGateWaypoint.Construction.Materials.Where(m => m.Fulfilled < m.Required).Any(m => i.Symbol == m.TradeSymbol)))
        {
            return true;
        }

        if (!ships.Any(s => s.ShipCommand?.ShipCommandEnum == ShipCommandEnum.SupplyConstruction)
            && agent.Credits > 800_000)
        {
            var inventory = ship.Cargo.Inventory;

            var otherShips = ships.Where(s => s.Symbol != ship.Symbol).ToList();
            if (otherShips.Any(s => s.Cargo.Inventory.Any(i => unfinishedJumpGateWaypoint.Construction.Materials.Where(m => m.Fulfilled < m.Required).Any(m => i.Symbol == m.TradeSymbol))))
            {
                return false;
            }

            if (inventory.Count == 0)
            {
                return true;
            }
        }
        return false;
    }
}