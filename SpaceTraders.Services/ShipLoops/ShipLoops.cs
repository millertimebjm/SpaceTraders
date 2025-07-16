using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.ShipLoops;

public class ShipLoopsService : IShipLoopsService
{
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly IAgentsService _agentsService;
    private readonly IShipsService _shipsService;
    private readonly ISystemsService _systemsService;
    private readonly IShipJobsFactory _shipJobsFactory;
    private readonly IShipCommandsServiceFactory _shipCommandsServiceFactory;

    public ShipLoopsService(
        IShipStatusesCacheService shipStatusesCacheService,
        IAgentsService agentsService,
        IShipsService shipsService,
        ISystemsService systemsService,
        IShipJobsFactory shipJobsFactory,
        IShipCommandsServiceFactory shipCommandsServiceFactory
    )
    {
        _shipStatusesCacheService = shipStatusesCacheService;
        _agentsService = agentsService;
        _shipsService = shipsService;
        _systemsService = systemsService;
        _shipJobsFactory = shipJobsFactory;
        _shipCommandsServiceFactory = shipCommandsServiceFactory;
    }

    public async Task Run()
    {
        await _shipStatusesCacheService.DeleteAsync();
        await _agentsService.GetAsync(refresh: true);

        // set ship jobs
        // recheck and reset ship job at the end of each job (usually after selling)
        //   if mining ship, do mining
        //   if hauler, check if there's a supply construction needed
        //     if supply construction, do construction for lowest percentage, and make sure credits are available for purchase, otherwise buy/sell
        //     else buy/sell
        //   if command ship, check for system without ships, then buy/sell
        var shipsDictionary = (await _shipsService.GetAsync()).ToDictionary(s => s.Symbol, s => s);
        foreach (var shipItem in shipsDictionary)
        {
            var ship = shipItem.Value;
            await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, ship.ShipCommand?.ShipCommandEnum, ship.Cargo, "No instructions set.", DateTime.UtcNow));
        }

        var systems = shipsDictionary.Values.Select(s => s.Nav.SystemSymbol);
        foreach (var system in systems)
        {
            await _systemsService.GetAsync(system);
        }

        while (true)
        {
            DateTime? minimumDate = null;
            shipsDictionary = (await _shipStatusesCacheService.GetAsync()).ToDictionary(s => s.Ship.Symbol, s => s.Ship);
            var shipsDictionaryOrdered = shipsDictionary.OrderBy(sd => Enum.Parse<ShipRegistrationRolesEnum>(sd.Value.Registration.Role)).ToList();
            foreach (var shipItem in shipsDictionaryOrdered)
            {
                var ship = shipItem.Value;
                var shipJobsService = _shipJobsFactory.Get(Enum.Parse<ShipRegistrationRolesEnum>(ship.Registration.Role));
                if (shipJobsService is null)
                {
                    await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, null, ship.Cargo, "No instructions set.", DateTime.UtcNow));
                    continue;
                }
                var shipCommand = await shipJobsService.Get(shipsDictionary.Values, ship);
                ship = ship with { ShipCommand = shipCommand };
                shipsDictionary[ship.Symbol] = ship;
                var shipStatus = await _shipStatusesCacheService.GetAsync(ship.Symbol);
                if (shipStatus is null)
                {
                    await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, shipCommand?.ShipCommandEnum, ship.Cargo, "No instructions set.", DateTime.UtcNow));
                }
                if (ship.ShipCommand is null) continue;

                var shipCommandService = _shipCommandsServiceFactory.Get(ship.ShipCommand.ShipCommandEnum);
                var shipUpdate = await shipCommandService.Run(
                    shipsDictionary[ship.Symbol],
                    shipsDictionary);
                shipsDictionary[shipUpdate.Symbol] = shipUpdate;

                var shipUpdateDelay = ShipsService.GetShipCooldown(shipUpdate);
                if (shipUpdateDelay is not null)
                {
                    minimumDate = MinimumDate(minimumDate, DateTime.UtcNow.Add(shipUpdateDelay.Value));
                }

                await Task.Delay(2000);
            }

            if (minimumDate is not null && minimumDate > DateTime.UtcNow)
            {
                TimeSpan minimumTimeSpan = minimumDate.Value - DateTime.UtcNow;
                await Task.Delay((int)minimumTimeSpan.TotalMilliseconds);
            }
        }
    }
    
    public static DateTime? MinimumDate(DateTime? d1, DateTime? d2)
    {
        if (d1 is null) return d2;
        if (d2 is null) return d1;
        if (d1 < d2) return d1;
        return d2;
    }
}