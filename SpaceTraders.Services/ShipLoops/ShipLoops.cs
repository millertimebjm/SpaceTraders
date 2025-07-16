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

    // public async Task Run()
    // {
    //     await _shipStatusesCacheService.DeleteAsync();
    //     await _agentsService.GetAsync(refresh: true);

    //     // set ship jobs
    //     // recheck and reset ship job at the end of each job (usually after selling)
    //     //   if mining ship, do mining
    //     //   if hauler, check if there's a supply construction needed
    //     //     if supply construction, do construction for lowest percentage, and make sure credits are available for purchase, otherwise buy/sell
    //     //     else buy/sell
    //     //   if command ship, check for system without ships, then buy/sell
    //     var shipsDictionary = (await _shipsService.GetAsync()).ToDictionary(s => s.Symbol, s => s);
    //     foreach (var shipItem in shipsDictionary)
    //     {
    //         var ship = shipItem.Value;
    //         await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, "No instructions set.", DateTime.UtcNow));
    //     }

    //     var systems = shipsDictionary.Values.Select(s => s.Nav.SystemSymbol);
    //     foreach (var system in systems)
    //     {
    //         await _systemsService.GetAsync(system);
    //     }

    //     while (true)
    //     {
    //         DateTime? minimumDate = null;
    //         shipsDictionary = (await _shipStatusesCacheService.GetAsync()).ToDictionary(s => s.Ship.Symbol, s => s.Ship);
    //         var shipsDictionaryOrdered = shipsDictionary.OrderBy(sd => Enum.Parse<ShipRegistrationRolesEnum>(sd.Value.Registration.Role)).ToList();
    //         foreach (var shipItem in shipsDictionaryOrdered)
    //         {
    //             var ship = shipItem.Value;
    //             var shipJobsService = _shipJobsFactory.Get(Enum.Parse<ShipRegistrationRolesEnum>(ship.Registration.Role));
    //             if (shipJobsService is null)
    //             {
    //                 ship = ship with { ShipCommand = null };
    //                 await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, "No instructions set.", DateTime.UtcNow));
    //                 continue;
    //             }
    //             var shipCommand = await shipJobsService.Get(shipsDictionary.Values, ship);
    //             ship = ship with { ShipCommand = shipCommand };
    //             shipsDictionary[ship.Symbol] = ship;
    //             var shipStatus = await _shipStatusesCacheService.GetAsync(ship.Symbol);
    //             shipStatus = shipStatus with { Ship = ship };
    //             await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, "No instructions set.", DateTime.UtcNow));

    //             if (shipStatus is null)
    //             {
    //                 ship = ship with { ShipCommand = shipCommand };
    //                 await _shipStatusesCacheService.SetAsync(new ShipStatus(ship, "No instructions set.", DateTime.UtcNow));
    //             }
    //             if (ship.ShipCommand is null) continue;

    //             var shipCommandService = _shipCommandsServiceFactory.Get(ship.ShipCommand.ShipCommandEnum);

    //             try
    //             {
    //                 ship = shipsDictionary[ship.Symbol];
    //                 ship = await shipCommandService.Run(
    //                     shipsDictionary[ship.Symbol],
    //                     shipsDictionary);
    //                 ship = ship with { Error = null };
    //                 shipsDictionary[ship.Symbol] = ship;
    //                 var shipUpdateDelay = ShipsService.GetShipCooldown(ship);
    //                 if (shipUpdateDelay is not null)
    //                 {
    //                     minimumDate = MinimumDate(minimumDate, DateTime.UtcNow.Add(shipUpdateDelay.Value));
    //                 }
    //             }
    //             catch (Exception ex)
    //             {
    //                 var timeSpan = TimeSpan.FromMinutes(10);
    //                 ship = ship with
    //                 {
    //                     Error = ex.Message,
    //                     Cooldown = new Cooldown(ship.Symbol, (int)timeSpan.TotalSeconds, (int)timeSpan.TotalSeconds, DateTime.UtcNow.Add(timeSpan))
    //                 };
    //             }
                
    //             await Task.Delay(2000);
    //         }

    //         if (minimumDate is not null && minimumDate > DateTime.UtcNow)
    //         {
    //             TimeSpan minimumTimeSpan = minimumDate.Value - DateTime.UtcNow;
    //             await Task.Delay((int)minimumTimeSpan.TotalMilliseconds);
    //         }
    //     }
    // }

    public async Task Run()
    {
        var ships = await _shipsService.GetAsync();
        await _shipStatusesCacheService.DeleteAsync();
        foreach (var ship in ships)
        {
            var shipStatus = new ShipStatus(ship, "No instructions set.", DateTime.UtcNow);
            await _shipStatusesCacheService.SetAsync(shipStatus);
        }

        while (true)
        {
            var shipStatuses = (await _shipStatusesCacheService.GetAsync()).ToList();
            for (int i = 0; i < shipStatuses.Count(); i++)
            {
                var ship = shipStatuses[i].Ship;

                if (ship.ShipCommand is null)
                {
                    var shipJobsService = _shipJobsFactory.Get(Enum.Parse<ShipRegistrationRolesEnum>(shipStatuses[i].Ship.Registration.Role));
                    if (shipJobsService is null)
                    {
                        ship = ship with { ShipCommand = null };
                        shipStatuses[i] = shipStatuses[i] with { Ship = ship };
                        await _shipStatusesCacheService.SetAsync(shipStatuses[i]);
                        continue;
                    }

                    var shipCommand = await shipJobsService.Get(shipStatuses.Select(ss => ss.Ship).ToList(), shipStatuses[i].Ship);
                    ship = ship with { ShipCommand = shipCommand };
                    shipStatuses[i] = shipStatuses[i] with { Ship = ship };
                    await _shipStatusesCacheService.SetAsync(shipStatuses[i]);
                    if (shipCommand is null) continue;
                }

                var shipCommandService = _shipCommandsServiceFactory.Get(ship.ShipCommand.ShipCommandEnum);

                try
                {
                    ship = await shipCommandService.Run(
                        ship,
                        shipStatuses.ToDictionary(ss => ss.Ship.Symbol, ss => ss.Ship));
                    ship = ship with { Error = null };
                }
                catch (Exception ex)
                {
                    var timeSpan = TimeSpan.FromMinutes(10);
                    ship = ship with
                    {
                        Error = ex.Message,
                        Cooldown = new Cooldown(ship.Symbol, (int)timeSpan.TotalSeconds, (int)timeSpan.TotalSeconds, DateTime.UtcNow.Add(timeSpan))
                    };
                }

                await Task.Delay(2000);
            }

            TimeSpan? shortestCooldown = null;
            foreach (var shipStatus in shipStatuses)
            {
                var cooldown = ShipsService.GetShipCooldown(shipStatus.Ship);
                if (cooldown is null || cooldown.Value.TotalSeconds < 0)
                {
                    shortestCooldown = null;
                    break;
                }

                if (shortestCooldown is null || cooldown.Value < shortestCooldown.Value)
                    shortestCooldown = cooldown.Value;
            }
            if (shortestCooldown is not null
                && shortestCooldown.Value.TotalMilliseconds > 0)
            {
                await Task.Delay(shortestCooldown.Value);
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