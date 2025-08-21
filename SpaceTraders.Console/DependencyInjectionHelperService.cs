using Microsoft.Extensions.DependencyInjection;
using SpaceTraders.Services;
using SpaceTraders.Services.Agents;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Constructions;
using SpaceTraders.Services.Constructions.Interfaces;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.JumpGates;
using SpaceTraders.Services.JumpGates.Interfaces;
using SpaceTraders.Services.Marketplaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
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
        services.AddSingleton<IWaypointsCacheService, WaypointsCacheService>();
        services.AddSingleton<IMongoCollectionFactory, MongoCollectionFactory>();
        services.AddSingleton<ISystemsApiService, SystemsApiService>();
        services.AddSingleton<ISystemsCacheService, SystemsCacheService>();
        services.AddSingleton<IJumpGatesServices, JumpGatesServices>();
        services.AddSingleton<IConstructionsService, ConstructionsService>();
        services.AddSingleton<IShipCommandsHelperService, ShipCommandsHelperService>();
        services.AddSingleton<IShipCommandsServiceFactory, ShipCommandsServiceFactory>();
        services.AddSingleton<IShipStatusesCacheService, ShipStatusesCacheService>();
        services.AddSingleton<IShipJobsFactory, ShipJobsFactory>();
        services.AddSingleton<ISurveysCacheService, SurveysCacheService>();
        services.AddSingleton<IShipLoopsService, ShipLoopsService>();
        services.AddSingleton<IPathsService, PathsService>();
        services.AddSingleton<ITransactionsService, TransactionsServices>();

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
    }
}