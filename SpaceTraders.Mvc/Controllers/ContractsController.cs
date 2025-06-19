using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Services.Contracts;
using SpaceTraders.Services.Contracts.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ContractsController : Controller
{
    private readonly ILogger<ContractsController> _logger;
    private readonly IContractsService _contractsService;

    public ContractsController(
        ILogger<ContractsController> logger,
        IContractsService contractsService)
    {
        _logger = logger;
        _contractsService = contractsService;
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
        return View("~/Views/Contracts/Index.cshtml", new List<STContract> {contract});
    }
}
