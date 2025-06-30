using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using SpaceTraders.Console;
using SpaceTraders.Models;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Marketplaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Companion.ChatFrontendBackend")
            .WriteTo.Console() // Default to console logging
            .CreateLogger();

          IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddCommandLine(args)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .Build();

        var httpClient = new HttpClient();
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
        });

        IShipsService shipsService = new ShipsService(
            httpClient,
            configuration,
            loggerFactory.CreateLogger<ShipsService>());

        IContractsService contractsService = new ContractsService(
            loggerFactory.CreateLogger<ContractsService>(),
            configuration,
            httpClient);

        IWaypointsService waypointsService = new WaypointsService(
            httpClient,
            configuration,
            loggerFactory.CreateLogger<WaypointsService>()
        );

        IMarketplacesService marketplacesService = new MarketplacesService(
            httpClient,
            configuration,
            loggerFactory.CreateLogger<MarketplacesService>()
        );

        await AutomatedMining(
            shipsService,
            contractsService,
            waypointsService,
            marketplacesService,
            loggerFactory.CreateLogger<Program>(),
            configuration);
    }

    public static async Task AutomatedMining(
            IShipsService shipsService,
            IContractsService contractsService,
            IWaypointsService waypointsService,
            IMarketplacesService marketplacesService,
            ILogger<Program> logger,
            IConfiguration configuration)
    {
        var shipSymbol = configuration[EnvironmentVariablesEnum.ShipSymbol.ToString()];
        Ship? ship = null;
        if (string.IsNullOrWhiteSpace(shipSymbol))
        {
            Console.WriteLine("Please enter ship symbol:");
            shipSymbol = Console.ReadLine();
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        ship = await shipsService.GetAsync(shipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(ship.Symbol);
        logger.LogInformation("ShipSymbol is {shipSymbol}", ship.Symbol);

        var miningWaypointSymbol= configuration[EnvironmentVariablesEnum.MiningWaypointSymbol.ToString()];
        if (string.IsNullOrWhiteSpace(miningWaypointSymbol))
        {
            Console.WriteLine("Mining Waypoint Symbol:");
            miningWaypointSymbol = Console.ReadLine();
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(miningWaypointSymbol);
        var miningWaypoint = await waypointsService.GetAsync(miningWaypointSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(miningWaypoint.Symbol);
        logger.LogInformation("MiningWaypointSymbol is {miningWaypointSymbol}", miningWaypoint.Symbol);

        var marketWaypointSymbol = configuration[EnvironmentVariablesEnum.MarketWaypointSymbol.ToString()];
        if (string.IsNullOrWhiteSpace(marketWaypointSymbol))
        {
            Console.WriteLine("Market Waypoint Symbol:");
            marketWaypointSymbol = Console.ReadLine();
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(marketWaypointSymbol);
        var marketWaypoint = await waypointsService.GetAsync(marketWaypointSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(marketWaypoint.Symbol);
        logger.LogInformation("MarketWaypointSymbol is {marketWaypointSymbol}", marketWaypoint.Symbol);


        while (true)
        {
            if (ship.Nav.WaypointSymbol == marketWaypoint.Symbol
                && ship.Cargo.Units > 0
                && ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString())
            {
                var marketplace = await marketplacesService.GetAsync(marketWaypoint.Symbol);
                foreach (var inventory in ship.Cargo.Inventory)
                {
                    if (!marketplace.TradeGoods.Select(tg => tg.Symbol).Contains(inventory.Symbol))
                    {
                        await shipsService.JettisonAsync(ship.Symbol, inventory.Symbol, inventory.Units);
                    }
                }
            }

            if (ship.Nav.WaypointSymbol == marketWaypoint.Symbol
                && (ship.Fuel.Current < ship.Fuel.Capacity
                    || ship.Cargo.Capacity == ship.Cargo.Units)
                && ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString())
            {
                await shipsService.DockAsync(ship.Symbol);
            }

            if (ship.Nav.WaypointSymbol == marketWaypoint.Symbol
                && ship.Fuel.Current < ship.Fuel.Capacity
                && ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
            {
                await marketplacesService.RefuelAsync(ship.Symbol);
            }

            if (ship.Nav.WaypointSymbol == marketWaypoint.Symbol
                && ship.Cargo.Units > 0
                && ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
            {
                var marketplace = await marketplacesService.GetAsync(marketWaypoint.Symbol);
                foreach (var inventory in ship.Cargo.Inventory)
                {
                    if (marketplace.TradeGoods?.Select(tg => tg.Symbol).Contains(inventory.Symbol) == true)
                    {
                        await marketplacesService.SellAsync(ship.Symbol, inventory.Symbol, inventory.Units);
                    }
                }
            }

            // Must be after Sell and Jettison
            if (ship.Nav.WaypointSymbol == marketWaypoint.Symbol
                && ship.Fuel.Current == ship.Fuel.Capacity
                && ship.Nav.Status == NavStatusEnum.DOCKED.ToString())
            {
                await shipsService.OrbitAsync(ship.Symbol);
            }

            // Must be after Sell and Jettison
            if (ship.Nav.WaypointSymbol == marketWaypoint.Symbol
                && ship.Fuel.Current == ship.Fuel.Capacity
                && ship.Nav.Status == NavStatusEnum.IN_ORBIT.ToString())
            {
                await shipsService.NavigateAsync(miningWaypoint.Symbol, ship.Symbol);
            }

            if (ship.Nav.WaypointSymbol == miningWaypoint.Symbol
                && ship.Cargo.Units < ship.Cargo.Capacity)
            {
                await shipsService.ExtractAsync(ship.Symbol);
            }

            if (ship.Nav.WaypointSymbol == miningWaypoint.Symbol
                && ship.Cargo.Units == ship.Cargo.Capacity)
            {
                await shipsService.NavigateAsync(marketWaypoint.Symbol, ship.Symbol);
            }

            ship = await shipsService.GetAsync(ship.Symbol);
            var shipCooldown = ShipsService.GetShipCooldown(ship);
            if (shipCooldown.HasValue && shipCooldown.Value.TotalSeconds > 0)
            {
                shipCooldown.Value.Add(TimeSpan.FromSeconds(1));
                await Task.Delay(shipCooldown.Value);
            }
        }
    }
}



