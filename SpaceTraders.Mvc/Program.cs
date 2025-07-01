using Serilog;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services;
using SpaceTraders.Services.Agents;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Marketplaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Shipyards.Interfaces;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
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
builder.Services.AddScoped<IMongoCollectionFactory, MongoCollectionFactory>();
builder.Services.AddScoped<ISystemsApiService, SystemsApiService>();
builder.Services.AddScoped<ISystemsCacheService, SystemsCacheService>();
builder.Services.AddScoped<ISystemsAsyncRefreshService, SystemsAsyncRefreshService>();

builder.Services.AddLogging();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Companion.ChatFrontendBackend")
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console() // Default to console logging
    .CreateLogger();
builder.Host.UseSerilog();

builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
var accountToken = builder.Configuration[ConfigurationEnums.AccountToken.ToString()];
ArgumentException.ThrowIfNullOrWhiteSpace(accountToken);
var agentToken = builder.Configuration[ConfigurationEnums.AgentToken.ToString()];
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
