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
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Console;

public static class DependencyInjectionHelperService
{
    public static void AddDependencies(IServiceCollection services)
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
        services.AddSingleton<IMongoCollectionFactory, MongoCollectionFactory>();
        services.AddScoped<ISystemsApiService, SystemsApiService>();
        services.AddScoped<ISystemsCacheService, SystemsCacheService>();
        services.AddScoped<IJumpGatesServices, JumpGatesServices>();
        services.AddScoped<IConstructionsService, ConstructionsService>();
        services.AddScoped<IShipCommandsHelperService, ShipCommandsHelperService>();
        services.AddScoped<IShipCommandsServiceFactory, ShipCommandsServiceFactory>();
        services.AddScoped<IShipStatusesCacheService, ShipStatusesCacheService>();
        services.AddScoped<IShipJobsFactory, ShipJobsFactory>();
        services.AddScoped<ISurveysCacheService, SurveysCacheService>();
        services.AddScoped<IShipLoopsService, ShipLoopsService>();
        services.AddScoped<IPathsService, PathsService>();

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
    }
}