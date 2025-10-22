using Microsoft.Extensions.DependencyInjection;
using SpaceTraders.Services.Agents;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Constructions;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.IoWrappers;
using SpaceTraders.Services.IoWrappers.Interfaces;
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
    public static void AddDependencies(IServiceCollection services)
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
        services.AddSingleton<ISurveysCacheService, SurveysMongoCacheService>();
        services.AddSingleton<IShipLoopsService, ShipLoopsService>();
        services.AddSingleton<IPathsService, PathsService>();
        services.AddSingleton<ITransactionsService, TransactionsServices>();
        services.AddSingleton<ITradesService, TradesService>();

        // Ship Commands
        services.AddSingleton<MiningToSellAnywhereCommand>();
        services.AddSingleton<BuyAndSellCommandV2>();
        services.AddSingleton<SupplyConstructionCommand>();
        services.AddSingleton<SurveyCommand>();
        services.AddSingleton<PurchaseShipCommand>();
        services.AddSingleton<ExplorationCommand>();

        // Ship Jobs
        services.AddSingleton<HaulerShipJobService>();
        services.AddSingleton<MiningShipJobService>();
        services.AddSingleton<CommandShipJobService>();
        services.AddSingleton<SurveyorShipJobService>();

        // Mongo Cache
        // services.AddSingleton<IMongoCollectionFactory, MongoCollectionFactory>();
        // services.AddSingleton<IWaypointsCacheService, WaypointsMongoCacheService>();
        // services.AddSingleton<ISystemsCacheService, SystemsMongoCacheService>();
        // services.AddSingleton<IShipStatusesCacheService, ShipStatusesMongoCacheService>();

        // File Cache
        services.AddSingleton<IFileWrapper, FileWrapper>();
        services.AddSingleton<IWaypointsCacheService, WaypointsFileCacheService>();
        services.AddSingleton<ISystemsCacheService, SystemsFileCacheService>();
        services.AddSingleton<IShipStatusesCacheService, ShipStatusesFileCacheService>();
        services.AddSingleton<ITradeModelCacheService, TradeModelFileCacheService>();
    }
}