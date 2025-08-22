using System.Text.Json;
using Serilog;
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
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Surveys;
using SpaceTraders.Services.Surveys.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Trades;
using SpaceTraders.Services.Transactions;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpClient();
builder.Services.AddScoped<IAgentsService, AgentsService>();
builder.Services.AddScoped<ISystemsService, SystemsService>();
builder.Services.AddScoped<IContractsService, ContractsService>();
builder.Services.AddScoped<IShipyardsService, ShipyardsService>();
builder.Services.AddScoped<IShipsService, ShipsService>();
builder.Services.AddScoped<IWaypointsService, WaypointsService>();
builder.Services.AddScoped<IMarketplacesService, MarketplacesService>();
builder.Services.AddScoped<IWaypointsApiService, WaypointsApiService>();
builder.Services.AddScoped<IWaypointsCacheService, WaypointsCacheService>();
builder.Services.AddSingleton<IMongoCollectionFactory, MongoCollectionFactory>();
builder.Services.AddScoped<ISystemsApiService, SystemsApiService>();
builder.Services.AddScoped<ISystemsCacheService, SystemsCacheService>();
builder.Services.AddScoped<IJumpGatesServices, JumpGatesServices>();
builder.Services.AddScoped<IConstructionsService, ConstructionsService>();
builder.Services.AddScoped<IShipStatusesCacheService, ShipStatusesCacheService>();
builder.Services.AddScoped<ISurveysCacheService, SurveysCacheService>();
builder.Services.AddScoped<ITransactionsService, TransactionsServices>();
builder.Services.AddScoped<ITradesService, TradesService>();

builder.Services.AddLogging();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Companion.ChatFrontendBackend")
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console() // Default to console logging
    .CreateLogger();
builder.Host.UseSerilog();


builder
    .Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

const string _appConfigSectionName = "SpaceTrader";
const string _appConfigEnvironmentVariableName = "AppConfigConnectionString";

string appConfigConnectionString =
            // Windows config value
            builder.Configuration[_appConfigEnvironmentVariableName]
            // Linux config value
            ?? builder.Configuration[$"Values:{_appConfigEnvironmentVariableName}"]
            ?? throw new ArgumentNullException(_appConfigEnvironmentVariableName);

if (appConfigConnectionString == null) throw new ArgumentNullException(nameof(appConfigConnectionString));

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddAzureAppConfiguration(appConfigConnectionString)
    .Build();

builder.Services.AddOptions<IConfiguration>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection(_appConfigSectionName).Bind(settings);
    });

// builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
var accountToken = builder.Configuration[$"{_appConfigSectionName}:{ConfigurationEnums.AccountToken.ToString()}"];
ArgumentException.ThrowIfNullOrWhiteSpace(accountToken);
var agentToken = builder.Configuration[$"{_appConfigSectionName}:{ConfigurationEnums.AgentToken.ToString()}"];
ArgumentException.ThrowIfNullOrWhiteSpace(agentToken);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
