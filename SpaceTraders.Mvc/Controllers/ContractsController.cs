using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ContractsController : BaseController
{
    private readonly ILogger<ContractsController> _logger;
    private readonly IContractsService _contractsService;
    private readonly IShipsService _shipsService;
    private readonly IAgentsService _agentsService;

    public ContractsController(
        ILogger<ContractsController> logger,
        IContractsService contractsService,
        IShipsService shipsService,
        IAgentsService agentsService) : base(agentsService)
    {
        _logger = logger;
        _contractsService = contractsService;
        _shipsService = shipsService;
        _agentsService = agentsService;
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
                            var unitsToDeliver = Math.Min(inventory.Units, deliver.UnitsRequired - deliver.UnitsFulfilled);

                            await _contractsService.DeliverAsync(
                                contractId,
                                ship.Symbol,
                                deliver.TradeSymbol,
                                unitsToDeliver);
                        }
                    }
                }
            }
        }

        return RedirectToAction("Index");
    }

    [Route("/contracts/{contractId}/fulfill")]
    public async Task<IActionResult> Fulfill(string contractId)
    {
        await _contractsService.FulfillAsync(contractId);
        var agent = await _agentsService.GetAsync();
        SessionHelper.Set(HttpContext, SessionEnum.CurrentCredits, agent.Credits);
        return RedirectToAction("Index");
    }

    [Route("/contracts/{shipSymbol}/negotiate")]
    public async Task<IActionResult> Negotiate(string shipSymbol)
    {
        await _contractsService.NegotiateAsync(shipSymbol);
        var agent = await _agentsService.GetAsync();
        SessionHelper.Set(HttpContext, SessionEnum.CurrentCredits, agent.Credits);
        return RedirectToAction("Index");
    }
}
