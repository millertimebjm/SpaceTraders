using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
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

        await AutomatedMining(shipsService, loggerFactory.CreateLogger<Program>());
    }

    public static async Task AutomatedMining(
            IShipsService shipsService,
            ILogger<Program> logger)
    {
        Console.WriteLine("Please enter ship symbol:");
        var shipSymbol = Console.ReadLine();
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        var ship = await shipsService.GetAsync(shipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(ship.Symbol);

        Console.WriteLine("Please enter Inventory item:");
        var inventoryName = Console.ReadLine();
        ArgumentException.ThrowIfNullOrWhiteSpace(inventoryName);
        if (!InventoryEnum.Items.Contains(inventoryName)) throw new ArgumentException("Inventory name is unknown.");

        Console.WriteLine("Please enter Inventory Units:");
        var inventoryUnitsString = Console.ReadLine();
        ArgumentException.ThrowIfNullOrWhiteSpace(inventoryUnitsString);
        var unitCount = int.Parse(inventoryUnitsString);

        do
        {
            var unwantedInventory = ship.Cargo.Inventory.Where(i => !InventoryEnum.Items.ToList().Contains(i.Symbol));
            foreach (var unwantedItem in unwantedInventory)
            {
                await shipsService.JettisonAsync(shipSymbol, unwantedItem.Symbol, unwantedItem.Units);
            }
            if (ship.Cooldown.RemainingSeconds > 0)
            {
                await Task.Delay((ship.Cooldown.RemainingSeconds + 1) * 1000);
            }
            await shipsService.ExtractAsync(shipSymbol);
            ship = await shipsService.GetAsync(shipSymbol);
            logger.LogInformation("{shipSymbol} ship is harvesting {inventoryName} and is at {units}/{unitCount}",
                shipSymbol,
                inventoryName,
                ship.Cargo.Inventory.SingleOrDefault(i => i.Symbol == inventoryName)?.Units ?? 0,
                unitCount);
        } while ((ship.Cargo.Inventory.SingleOrDefault(i => i.Symbol == inventoryName)?.Units ?? 0) < unitCount);
    }
}



