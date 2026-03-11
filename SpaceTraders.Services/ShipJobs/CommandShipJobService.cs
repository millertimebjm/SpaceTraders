using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class CommandShipJobService(
    IAgentsService _agentsService,
    ISystemsService _systemsService,
    IShipCommandsHelperService _shipCommandHelperService,
    IShipyardsService _shipyardsService,
    IShipStatusesCacheService _shipStatusesCacheService
) : IShipJobService
{
    public async Task<ShipCommand?> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var agent = await _agentsService.GetAsync();
        var systems = await _systemsService.GetAsync();
        var traversableSystems = SystemsService.Traverse(systems, ship.Nav.SystemSymbol);
        var waypoints = traversableSystems.SelectMany(s => s.Waypoints).ToList();
    
        var (shipyardWaypoint, shipType) = await _shipCommandHelperService.ShipToBuy(ships);
        if (shipType is not null && shipyardWaypoint is not null)
        {
            if(!await _shipCommandHelperService.CheckRemotePurchaseShip(ships, shipyardWaypoint, shipType.Value))
            {
                return new ShipCommand(ship.Symbol, ShipCommandEnum.PurchaseShip);
            }
        }

        if (!ships.Any(s => s.Registration.Role == ShipRegistrationRolesEnum.HAULER.ToString()))
        {
            return new ShipCommand(ship.Symbol, ShipCommandEnum.FulfillContract);
        }
        
        return new ShipCommand(ship.Symbol, ShipCommandEnum.BuyToSell);
    }
}