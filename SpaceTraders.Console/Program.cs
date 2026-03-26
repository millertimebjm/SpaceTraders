using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SpaceTraders.Console;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers.Interfaces;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.ShipLogs.Interfaces;

public class Program
{
    public static async Task Main(string[] args)
    {
        const string _appConfigEnvironmentVariableName = "AppConfigConnectionString";

        var host = Host.CreateDefaultBuilder(args);
        host.ConfigureAppConfiguration((context, config) =>
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string appConfigConnectionString =
                // Windows config value
                configuration[_appConfigEnvironmentVariableName]
                // Linux config value
                ?? configuration[$"Values:{_appConfigEnvironmentVariableName}"]
                ?? throw new ArgumentNullException(_appConfigEnvironmentVariableName);

            ArgumentException.ThrowIfNullOrWhiteSpace(appConfigConnectionString);
            configuration = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddAzureAppConfiguration(appConfigConnectionString)
                .Build();

            const string _appConfigSectionName = "SpaceTrader";
            var accountToken = configuration[$"{_appConfigSectionName}:{ConfigurationEnums.AccountToken.ToString()}"];
            ArgumentException.ThrowIfNullOrWhiteSpace(accountToken);

            config
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddAzureAppConfiguration(appConfigConnectionString);
        });
        host.ConfigureServices((context, services) =>
        {
            DependencyInjectionHelperService.AddDependencies(services);
        });
        var serviceBuilder = host.Build();
        
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Companion.ChatFrontendBackend")
            .WriteTo.Console() // Default to console logging
            .CreateLogger();

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1)); 
            var shipLogsService = serviceBuilder.Services.GetRequiredService<IShipLogsService>();

            while (await timer.WaitForNextTickAsync(CancellationToken.None))
            {
                await shipLogsService.WriterAsync();
            }
        });

        _ = Task.Run(async () =>
        {
            var dispatcher = serviceBuilder.Services.GetRequiredService<IApiRequestLimiterService>();
            await dispatcher.ProcessQueueAsync(CancellationToken.None);
        });

        var shipLoopsService = serviceBuilder.Services.GetRequiredService<IShipLoopsService>();
        await shipLoopsService.Run();
    }
}