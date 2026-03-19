using SpaceTraders.Models;
using SpaceTraders.Services.Agents;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Services.ShipJobs.Interfaces;

public class MiningShipJobService(IAgentsService _agentsService, ISystemsService _systemsService) : IShipJobService
{
    public async Task<ShipCommand> Get(
        IEnumerable<Ship> ships,
        Ship ship)
    {
        var agent = await _agentsService.GetAsync();
        var headquarters = agent.Headquarters;
        var homeSystem = await _systemsService.GetAsync(WaypointsService.ExtractSystemFromWaypoint(agent.Headquarters));
        var jumpGate = homeSystem.Waypoints.Single(w => w.JumpGate is not null);
        if (!jumpGate.IsUnderConstruction)
        {
            return new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.ScrapShip);
        }
        return new ShipCommand(ship.Symbol, Models.Enums.ShipCommandEnum.MiningToSellAnywhere);
    }
}