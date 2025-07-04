using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SpaceTraders.Models;
using SpaceTraders.Services;
using SpaceTraders.Services.Agents;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Constructions;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.JumpGates;
using SpaceTraders.Services.JumpGates.Interfaces;
using SpaceTraders.Services.Marketplaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.ShipCommands;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args);
        host.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        });
        host.ConfigureServices((context, services) =>
        {
            services.AddHttpClient();
            services.AddScoped<IAgentsService, AgentsService>();
            services.AddScoped<ISystemsService, SystemsService>();
            services.AddScoped<IContractsService, ContractsService>();
            services.AddScoped<IShipyardsService, ShipyardsService>();
            services.AddScoped<IShipsService, ShipsService>();
            services.AddScoped<IWaypointsService, WaypointsService>();
            services.AddScoped<IMarketplacesService, MarketplacesService>();
            services.AddScoped<IWaypointsApiService, WaypointsApiService>();
            services.AddScoped<IWaypointsCacheService, WaypointsCacheService>();
            services.AddScoped<IMongoCollectionFactory, MongoCollectionFactory>();
            services.AddScoped<ISystemsApiService, SystemsApiService>();
            services.AddScoped<ISystemsCacheService, SystemsCacheService>();
            services.AddScoped<IJumpGatesServices, JumpGatesServices>();
            services.AddScoped<IConstructionsService, ConstructionsService>();
            services.AddScoped<IShipCommandsHelperService, ShipCommandsHelperService>();
            services.AddScoped<MiningToSellAnywhereCommand>();
            services.AddScoped<BuyAndSellCommand>();
            services.AddScoped<SupplyConstructionCommand>();
            services.AddScoped<IShipCommandsServiceFactory, ShipCommandsServiceFactory>();
            services.AddScoped<IShipStatusesCacheService, ShipStatusesCacheService>();

        });
        var serviceBuilder = host.Build();

        var waypointsCacheService = serviceBuilder.Services.GetRequiredService<IWaypointsCacheService>();
        var waypointsApiService = serviceBuilder.Services.GetRequiredService<IWaypointsApiService>();
        var marketplacesService = serviceBuilder.Services.GetRequiredService<IMarketplacesService>();
        var shipyardsService = serviceBuilder.Services.GetRequiredService<IShipyardsService>();
        var shipsService = serviceBuilder.Services.GetRequiredService<IShipsService>();
        var contractsService = serviceBuilder.Services.GetRequiredService<IContractsService>();
        var waypointsService = serviceBuilder.Services.GetRequiredService<IWaypointsService>();
        var shipCommandsServiceFactory = serviceBuilder.Services.GetRequiredService<IShipCommandsServiceFactory>();
        var shipStatusesCacheService = serviceBuilder.Services.GetRequiredService<IShipStatusesCacheService>();

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

        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
        });
        var ships = await shipsService.GetAsync();
        var shipCommands = configuration.GetSection("ShipCommands").Get<List<ShipCommand>>();
        await shipStatusesCacheService.DeleteAsync();
        foreach (var ship in ships)
        {
            var shipCommand = shipCommands.SingleOrDefault(sc => sc.ShipSymbol == ship.Symbol);
            await shipStatusesCacheService.SetAsync(new ShipStatus(ship.Symbol, shipCommand?.ShipCommandEnum, ship.Cargo, "No instructions set.", DateTime.UtcNow));
        }
        ArgumentNullException.ThrowIfNull(shipCommands);
        while (true)
        {
            DateTime? minimumDelay = null;
            
            foreach (var shipCommand in shipCommands)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(shipCommand.StartWaypointSymbol);
                var startWaypoint = await waypointsService.GetAsync(shipCommand.StartWaypointSymbol);
                var shipCommandService = shipCommandsServiceFactory.Get(shipCommand.ShipCommandEnum.ToString());
                var tempDelay = await shipCommandService.Run(
                    shipCommand.ShipSymbol,
                    startWaypoint);

                minimumDelay = MinimumDate(minimumDelay, tempDelay);

                await Task.Delay(2000);
            }

            if (minimumDelay is not null && minimumDelay > DateTime.UtcNow)
            {
                TimeSpan minimumTimeSpan = minimumDelay.Value - DateTime.UtcNow;
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



