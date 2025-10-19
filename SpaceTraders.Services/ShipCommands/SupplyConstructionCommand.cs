using SpaceTraders.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.ShipCommands;

public class SupplyConstructionCommand(
    IShipCommandsHelperService _shipCommandsHelperService,
    IWaypointsService _waypointsService,
    ISystemsService _systemsService,
    IAgentsService _agentsService,
    IWaypointsCacheService _waypointsCacheService,
    ITransactionsService _transactionsService
) : IShipCommandsService
{
    public async Task<ShipStatus> Run(
        ShipStatus shipStatus,
        Dictionary<string, Ship> shipsDictionary)
    {
        var loopCount = 0;
        var ship = shipStatus.Ship;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        var system = await _systemsService.GetAsync(ship.Nav.SystemSymbol);
        var constructionWaypoint = system.Waypoints.FirstOrDefault(w => w.JumpGate is not null && w.IsUnderConstruction);
        while (true)
        {
            loopCount++;
            if (loopCount > 20) return new ShipStatus(ship, $"Ship in loop.", DateTime.UtcNow);
            if (ShipsService.GetShipCooldown(ship) is not null) return shipStatus;
            var paths = PathsService.BuildWaypointPath(system.Waypoints, currentWaypoint, ship.Fuel.Capacity, ship.Fuel.Current);

            await Task.Delay(1000);

            // var cargo = await _shipCommandsHelperService.JettisonForSupplyConstruction(ship, constructionWaypoint);
            // if (cargo is not null)
            // {
            //     ship = ship with { Cargo = cargo };
            //     continue;
            // }

            var refuelResponse = await _shipCommandsHelperService.Refuel(ship, currentWaypoint);
            if (refuelResponse is not null)
            {
                ship = ship with { Fuel = refuelResponse.Fuel };
                await _agentsService.SetAsync(refuelResponse.Agent);
                await _transactionsService.SetAsync(refuelResponse.Transaction);
                continue;
            }

            var nav = await _shipCommandsHelperService.DockForSupplyConstruction(ship, currentWaypoint, constructionWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol, refresh: true);
                continue;
            }

            var supplyResult = await _shipCommandsHelperService.SupplyConstructionSite(ship, currentWaypoint);
            if (supplyResult is not null)
            {
                ship = ship with { Cargo = supplyResult.Cargo };
                currentWaypoint = currentWaypoint with { Construction = supplyResult.Construction };
                await _waypointsCacheService.SetAsync(currentWaypoint);

                if (currentWaypoint.Construction.Materials.All(m => m.Fulfilled == m.Required)
                    || ship.Cargo.Units == 0)
                {
                    ship = ship with { ShipCommand = null };
                    currentWaypoint = await _waypointsService.GetAsync(currentWaypoint.Symbol, refresh: true);
                    return new ShipStatus(ship, $"Resetting Job.", DateTime.UtcNow);
                }
            
                continue;
            }

            var purchaseCargoResult = await _shipCommandsHelperService.BuyForConstruction(ship, currentWaypoint, constructionWaypoint);
            if (purchaseCargoResult is not null)
            {
                ship = ship with { Cargo = purchaseCargoResult.Cargo };
                await _agentsService.SetAsync(purchaseCargoResult.Agent);
                await _transactionsService.SetAsync(purchaseCargoResult.Transaction);
                continue;
            }

            nav = await _shipCommandsHelperService.Orbit(ship, currentWaypoint);
            if (nav is not null)
            {
                ship = ship with { Nav = nav };
                continue;
            }

            (nav, var fuel) = await _shipCommandsHelperService.NavigateToConstructionWaypoint(ship, currentWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                return new ShipStatus(ship, $"NavigateToStartWaypoint {constructionWaypoint.Symbol}", DateTime.UtcNow);
            }

            (nav, fuel) = await _shipCommandsHelperService.NavigateToMarketplaceExport(ship, currentWaypoint, constructionWaypoint);
            if (nav is not null && fuel is not null)
            {
                ship = ship with { Nav = nav, Fuel = fuel };
                return new ShipStatus(ship, $"NavigateToMarketplaceExport {nav.Route.Destination.Symbol}", DateTime.UtcNow);
            }

            throw new Exception($"Infinite loop, no work planned. {ship.Symbol}, {currentWaypoint.Symbol}, {string.Join(":", ship.Cargo.Inventory.Select(i => $"{i.Name}/{i.Units}"))}, {ship.Fuel.Current}/{ship.Fuel.Capacity}");

        }
    }
}