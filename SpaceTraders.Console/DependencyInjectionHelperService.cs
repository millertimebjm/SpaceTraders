using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpaceTraders.Services.Agents;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Constructions;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.EntityFrameworkCache;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.JumpGates;
using SpaceTraders.Services.JumpGates.Interfaces;
using SpaceTraders.Services.Marketplaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.MongoCache;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.ShipCommands;
using SpaceTraders.Services.ShipCommands.Interfaces;
using SpaceTraders.Services.ShipJobs;
using SpaceTraders.Services.ShipJobs.Interfaces;
using SpaceTraders.Services.ShipLoops;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Surveys;
using SpaceTraders.Services.Surveys.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Trades.Interfaces;
using SpaceTraders.Services.Transactions;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Console;

public static class DependencyInjectionHelperService
{
    public static async Task AddDependencies(IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<IAgentsService, AgentsService>();
        services.AddSingleton<ISystemsService, SystemsService>();
        services.AddSingleton<IContractsService, ContractsService>();
        services.AddSingleton<IShipyardsService, ShipyardsService>();
        services.AddSingleton<IShipsService, ShipsService>();
        services.AddSingleton<IWaypointsService, WaypointsService>();
        services.AddSingleton<IMarketplacesService, MarketplacesService>();
        services.AddSingleton<IWaypointsApiService, WaypointsApiService>();
        services.AddSingleton<ISystemsApiService, SystemsApiService>();
        services.AddSingleton<IJumpGatesServices, JumpGatesServices>();
        services.AddSingleton<IConstructionsService, ConstructionsService>();
        services.AddSingleton<IShipCommandsHelperService, ShipCommandsHelperService>();
        services.AddSingleton<IShipCommandsServiceFactory, ShipCommandsServiceFactory>();
        services.AddSingleton<IShipJobsFactory, ShipJobsFactory>();
        services.AddSingleton<IShipLoopsService, ShipLoopsService>();
        services.AddSingleton<IPathsService, PathsService>();
        services.AddSingleton<ITradesService, TradesService>();

        // Ship Commands
        services.AddSingleton<MiningToSellAnywhereCommand>();
        services.AddSingleton<BuyAndSellCommand>();
        services.AddSingleton<SupplyConstructionCommand>();
        services.AddSingleton<SurveyCommand>();
        services.AddSingleton<PurchaseShipCommand>();
        services.AddSingleton<ExplorationCommand>();

        // Ship Jobs
        services.AddSingleton<HaulerShipJobService>();
        services.AddSingleton<MiningShipJobService>();
        services.AddSingleton<CommandShipJobService>();
        services.AddSingleton<SurveyorShipJobService>();

        // Cache Services
        services.AddSingleton<IMongoCollectionFactory, MongoCollectionFactory>();
        services.AddSingleton<IAgentsCacheService, AgentsCacheMongoService>();
        services.AddSingleton<IShipStatusesCacheService, ShipStatusesCacheMongoService>();
        services.AddSingleton<ISurveysCacheService, SurveysCacheMongoService>();
        services.AddSingleton<ISystemsCacheService, SystemsCacheMongoService>();
        services.AddSingleton<IWaypointsCacheService, WaypointsCacheMongoService>();
        services.AddSingleton<ITransactionsCacheService, TransactionsCacheMongoService>();
        services.AddSingleton<ITradesCacheService, TradesCacheMongoService>();
        services.AddSingleton<IPathsCacheService, PathsCacheMongoService>();

        // services.AddSingleton<IAgentsCacheService, AgentsCacheEfService>();
        // services.AddSingleton<IShipStatusesCacheService, ShipStatusesCacheEfService>();
        // services.AddSingleton<ISurveysCacheService, SurveysCacheEfService>();
        // services.AddSingleton<ISystemsCacheService, SystemsCacheEfService>();
        // services.AddSingleton<IWaypointsCacheService, WaypointsCacheEfService>();
        // services.AddSingleton<ITransactionsCacheService, TransactionsCacheEfServices>();
        // services.AddSingleton<ITradesCacheService, TradesCacheEfService>();

        // var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        // var sqlServerConnectionString = config["SpaceTrader:SqlServerConnectionString"];
        // ArgumentNullException.ThrowIfNullOrWhiteSpace(sqlServerConnectionString);
        // services.AddDbContextPool<SpaceTraderDbContext>(options => 
        //     options.UseSqlServer(sqlServerConnectionString));
        // var context = services.BuildServiceProvider().GetRequiredService<SpaceTraderDbContext>();
        // context.Database.EnsureCreated();
    }
}