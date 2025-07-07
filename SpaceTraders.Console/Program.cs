using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
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
using SpaceTraders.Services.ShipJobs;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Surveys;
using SpaceTraders.Services.Surveys.Interfaces;
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
            services.AddScoped<IShipCommandsServiceFactory, ShipCommandsServiceFactory>();
            services.AddScoped<IShipStatusesCacheService, ShipStatusesCacheService>();
            services.AddScoped<IShipJobsFactory, ShipJobsFactory>();
            services.AddScoped<ISurveysCacheService, SurveysCacheService>();

            // Ship Commands
            services.AddScoped<MiningToSellAnywhereCommand>();
            services.AddScoped<BuyAndSellCommand>();
            services.AddScoped<SupplyConstructionCommand>();
            services.AddScoped<SurveyCommand>();
            services.AddScoped<PurchaseShipCommand>();

            // Ship Jobs
            services.AddScoped<HaulerShipJobService>();
            services.AddScoped<MiningShipJobService>();
            services.AddScoped<CommandShipJobService>();
            services.AddScoped<SurveyorShipJobService>();


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
        var shipJobsFactory = serviceBuilder.Services.GetRequiredService<IShipJobsFactory>();

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

        await shipStatusesCacheService.DeleteAsync();

        // set ship jobs
        // recheck and reset ship job at the end of each job (usually after selling)
        //   if mining ship, do mining
        //   if hauler, check if there's a supply construction needed
        //     if supply construction, do construction for lowest percentage, and make sure credits are available for purchase, otherwise buy/sell
        //     else buy/sell
        //   if command ship, check for system without ships, then buy/sell
        var shipsDictionary = (await shipsService.GetAsync()).ToDictionary(s => s.Symbol, s => s);
        foreach (var shipItem in shipsDictionary)
        {
            var ship = shipItem.Value;
            var shipJobsService = shipJobsFactory.Get((ShipRegistrationRolesEnum)Enum.Parse(typeof(ShipRegistrationRolesEnum), ship.Registration.Role));
            if (shipJobsService is null)
            {
                await shipStatusesCacheService.SetAsync(new ShipStatus(ship, null, ship.Cargo, "No instructions set.", DateTime.UtcNow));
                continue;
            }
            var shipCommand = await shipJobsService.Get(shipsDictionary.Values, ship);
            ship = ship with { ShipCommand = shipCommand };
            shipsDictionary[ship.Symbol] = ship;
            await shipStatusesCacheService.SetAsync(new ShipStatus(ship, shipCommand?.ShipCommandEnum, ship.Cargo, "No instructions set.", DateTime.UtcNow));
        }

        // var shipCommands = configuration.GetSection("ShipCommands").Get<List<ShipCommand>>();

        // foreach (var ship in ships)
        // {
        //     var shipCommand = shipCommands.SingleOrDefault(sc => sc.ShipSymbol == ship.Symbol);
        //     await shipStatusesCacheService.SetAsync(new ShipStatus(ship, shipCommand?.ShipCommandEnum, ship.Cargo, "No instructions set.", DateTime.UtcNow));
        // }

        //ArgumentNullException.ThrowIfNull(shipCommands);
        while (true)
        {
            DateTime? minimumDate = null;
            
            foreach (var shipItem in shipsDictionary.OrderBy(sd => (ShipRegistrationRolesEnum)Enum.Parse(typeof(ShipRegistrationRolesEnum), sd.Value.Registration.Role)))
            {
                var ship = shipItem.Value;
                if (ship.ShipCommand is not null)
                {
                    //ArgumentException.ThrowIfNullOrWhiteSpace(ship.ShipCommand.StartWaypointSymbol);
                }
                else
                {
                    continue;
                }
                var shipCommandService = shipCommandsServiceFactory.Get(ship.ShipCommand.ShipCommandEnum);
                var shipUpdate = await shipCommandService.Run(
                    shipItem.Value,
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



