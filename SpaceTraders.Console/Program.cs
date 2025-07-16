using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SpaceTraders.Console;
using SpaceTraders.Services.Interfaces;
using SpaceTraders.Services.ShipLoops;

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
            DependencyInjectionHelperService.AddDependencies(services);
        });
        var serviceBuilder = host.Build();
        
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Companion.ChatFrontendBackend")
            .WriteTo.Console() // Default to console logging
            .CreateLogger();

        // IConfiguration configuration = new ConfigurationBuilder()
        //   .SetBasePath(Directory.GetCurrentDirectory())
        //   .AddCommandLine(args)
        //   .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        //   .Build();

        // ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        // {
        //     builder.AddSerilog();
        // });

        var shipLoopsService = serviceBuilder.Services.GetRequiredService<IShipLoopsService>();
        await shipLoopsService.Run();
    }
}