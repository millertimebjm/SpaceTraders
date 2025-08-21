using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SpaceTraders.Console;
using SpaceTraders.Services.Interfaces;

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
            // var accountToken = configuration[$"{_appConfigSectionName}:{ConfigurationEnums.AccountToken.ToString()}"];
            // ArgumentException.ThrowIfNullOrWhiteSpace(accountToken);
            // var agentToken = configuration[$"{_appConfigSectionName}:{ConfigurationEnums.AgentToken.ToString()}"];
            // ArgumentException.ThrowIfNullOrWhiteSpace(agentToken);

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

        // IConfiguration configuration = new ConfigurationBuilder()
        //   .SetBasePath(Directory.GetCurrentDirectory())
        //   .AddCommandLine(args)
        //   .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        //   .Build();

        // IConfiguration configuration = new ConfigurationBuilder()
        //     .SetBasePath(Directory.GetCurrentDirectory())
        //     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        //     .AddEnvironmentVariables()
        //     .Build();

        

        
        // configuration = new ConfigurationBuilder()
        //     .SetBasePath(Directory.GetCurrentDirectory())
        //     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
        //     .AddEnvironmentVariables()
        //     .AddAzureAppConfiguration(appConfigConnectionString)
        //     .Build();

        // configuration = configuration.AddOptions<IConfiguration>()
        //     .Configure<IConfiguration>((settings, configuration) =>
        //     {
        //         configuration.GetSection(_appConfigSectionName).Bind(settings);
        //     });

        // builder.Configuration.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        // var accountToken = configuration[$"{_appConfigSectionName}:{ConfigurationEnums.AccountToken.ToString()}"];
        // ArgumentException.ThrowIfNullOrWhiteSpace(accountToken);
        // var agentToken = configuration[$"{_appConfigSectionName}:{ConfigurationEnums.AgentToken.ToString()}"];
        // ArgumentException.ThrowIfNullOrWhiteSpace(agentToken);

        // ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        // {
        //     builder.AddSerilog();
        // });

        var shipLoopsService = serviceBuilder.Services.GetRequiredService<IShipLoopsService>();
        await shipLoopsService.Run();
    }
}