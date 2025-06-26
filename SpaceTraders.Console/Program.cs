using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Shipyards;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Companion.ChatFrontendBackend")
            .WriteTo.Console() // Default to console logging
            .CreateLogger();

          IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .Build();

        var httpClient = new HttpClient();
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();
        });

        IShipsService shipsService = new ShipsService(
            httpClient,
            configuration,
            loggerFactory.CreateLogger<ShipsService>());

        IContractsService contractsService = new ContractsService(
            loggerFactory.CreateLogger<ContractsService>(),
            configuration,
            httpClient);

        await AutomatedMining(
            shipsService,
            contractsService,
            loggerFactory.CreateLogger<Program>());
    }

    public static async Task AutomatedMining(
            IShipsService shipsService,
            IContractsService contractsService,
            ILogger<Program> logger)
    {
        Console.WriteLine("Please enter ship symbol:");
        var shipSymbol = Console.ReadLine();
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        var ship = await shipsService.GetAsync(shipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(ship.Symbol);

        Console.WriteLine("Please enter Contract Id:");
        var contractId = Console.ReadLine();
        ArgumentException.ThrowIfNullOrWhiteSpace(contractId);
        var contract = await contractsService.GetAsync(contractId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contract.Id);

        

        // do
        // {
        //     var unwantedInventory = ship.Cargo.Inventory.Where(i => i.Symbol != inventorySymbol);
        //     foreach (var unwantedItem in unwantedInventory)
        //     {
        //         await shipsService.JettisonAsync(shipSymbol, unwantedItem.Symbol, unwantedItem.Units);
        //     }
        //     if (ship.Cooldown.RemainingSeconds > 0)
        //     {
        //         await Task.Delay((ship.Cooldown.RemainingSeconds + 1) * 1000);
        //     }
        //     await shipsService.ExtractAsync(shipSymbol);
        //     ship = await shipsService.GetAsync(shipSymbol);
        //     logger.LogInformation("{shipSymbol} ship is harvesting {inventoryName} and is at {units}/{unitCount}",
        //         shipSymbol,
        //         inventorySymbol,
        //         ship.Cargo.Inventory.SingleOrDefault(i => i.Symbol == inventorySymbol)?.Units ?? 0,
        //         unitCount);
        // } while ((ship.Cargo.Inventory.SingleOrDefault(i => i.Symbol == inventorySymbol)?.Units ?? 0) < unitCount);
    }
}



