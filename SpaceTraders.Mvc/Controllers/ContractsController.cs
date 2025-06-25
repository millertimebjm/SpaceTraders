using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ContractsController : BaseController
{
    private readonly ILogger<ContractsController> _logger;
    private readonly IContractsService _contractsService;
    private readonly IShipsService _shipsService;

    public ContractsController(
        ILogger<ContractsController> logger,
        IContractsService contractsService,
        IShipsService shipsService)
    {
        _logger = logger;
        _contractsService = contractsService;
        _shipsService = shipsService;
    }

    public async Task<IActionResult> Index()
    {
        var contracts = await _contractsService.GetAsync();
        return View(contracts);
    }

    [Route("/contracts/accept/{contractId}")]
    public async Task<IActionResult> Accept(string contractId)
    {
        var contract = await _contractsService.AcceptAsync(contractId);
        return View("~/Views/Contracts/Index.cshtml", new List<STContract> { contract });
    }

    [Route("/contracts/{contractId}/deliver")]
    public async Task<IActionResult> Deliver(string contractId)
    {
        var contract = await _contractsService.GetAsync(contractId);
        var ships = await _shipsService.GetAsync();
        foreach (var deliver in contract.Terms.Deliver)
        {
            foreach (var ship in ships)
            {
                if (ship.Nav.WaypointSymbol == deliver.DestinationSymbol)
                {
                    foreach (var inventory in ship.Cargo.Inventory)
                    {
                        if (deliver.TradeSymbol == inventory.Symbol)
                        {
                            await _contractsService.DeliverAsync(
                                contractId, 
                                ship.Symbol,
                                deliver.TradeSymbol,
                                inventory.Units);
                        }
                    }
                }
            }
        }
        
        return RedirectToAction("Index");
    }
}
