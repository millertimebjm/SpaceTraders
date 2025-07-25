using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.Surveys.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipsController : BaseController
{
    private readonly ILogger<ShipsController> _logger;
    private readonly IShipsService _shipsService;
    private readonly IWaypointsService _waypointsService;
    private readonly IMarketplacesService _marketplacesService;
    private readonly IAgentsService _agentsService;
    private readonly IContractsService _contractsService;
    private readonly ISurveysCacheService _surveyCacheService;
    private readonly ITransactionsService _transactionsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;

    public ShipsController(
        ILogger<ShipsController> logger,
        IShipsService shipsService,
        IWaypointsService waypointsService,
        IMarketplacesService marketplacesService,
        IAgentsService agentsService,
        IContractsService contractsService,
        ISurveysCacheService surveysCacheService,
        ITransactionsService transactionsService,
        IShipStatusesCacheService shipStatusesCacheService) : base(agentsService)
    {
        _logger = logger;
        _shipsService = shipsService;
        _waypointsService = waypointsService;
        _marketplacesService = marketplacesService;
        _agentsService = agentsService;
        _contractsService = contractsService;
        _surveyCacheService = surveysCacheService;
        _transactionsService = transactionsService;
        _shipStatusesCacheService = shipStatusesCacheService;
    }

    [Route("/ships")]
    public IActionResult Index()
    {
        ShipsViewModel model = new(
            _shipsService.GetAsync(),
            _contractsService.GetActiveAsync());
        return View(model);
    }

    [Route("/ships/{shipSymbol}/active")]
    public async Task<IActionResult> SetActive(string shipSymbol)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        SessionHelper.Set(HttpContext, SessionEnum.CurrentShipSymbol, ship.Symbol);
        var waypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        SessionHelper.Set(HttpContext, SessionEnum.CurrentWaypointSymbol, waypoint.Symbol);
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Ship",
            shipSymbol
        });
    }

    [Route("/ships/{shipSymbol}/orbit")]
    public async Task<IActionResult> Orbit(string shipSymbol)
    {
        var nav = await _shipsService.OrbitAsync(shipSymbol);
        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/{shipSymbol}/dock")]
    public async Task<IActionResult> Dock(string shipSymbol)
    {
        var nav = await _shipsService.DockAsync(shipSymbol);
        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/extract")]
    public async Task<IActionResult> Extract()
    {
        var shipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        await _shipsService.ExtractAsync(shipSymbol);
        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/survey")]
    public async Task<IActionResult> Survey()
    {
        var shipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        var (_, surveys) = await _shipsService.SurveyAsync(shipSymbol);
        foreach (var survey in surveys)
        {
            await _surveyCacheService.SetAsync(survey);
        }
        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/{shipSymbol}")]
    public IActionResult Ship(string shipSymbol)
    {
        ShipViewModel model = new(
            _shipsService.GetAsync(shipSymbol),
            _contractsService.GetActiveAsync());
        return View(model);
    }

    [Route("/ships/{shipSymbol}/jettison/{inventorySymbol}")]
    public async Task<IActionResult> Jettison(string shipSymbol, string inventorySymbol)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        await _shipsService.JettisonAsync(shipSymbol, inventorySymbol, ship.Cargo.Inventory.Single(i => i.Symbol == inventorySymbol).Units);
        ship = await _shipsService.GetAsync(shipSymbol);
        return Redirect($"/ships/{ship.Symbol}");
    }

    [Route("/ships/deactivate")]
    public IActionResult Deactivate()
    {
        SessionHelper.Unset(HttpContext, SessionEnum.CurrentShipSymbol);
        SessionHelper.Unset(HttpContext, SessionEnum.CurrentWaypointSymbol);
        return RedirectToAction("Index");
    }

    [Route("/ships/{shipSymbol}/fuel")]
    public async Task<IActionResult> Refuel(string shipSymbol)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        await _marketplacesService.RefuelAsync(shipSymbol);
        var agent = await _agentsService.GetAsync();
        SessionHelper.Set(HttpContext, SessionEnum.CurrentCredits, agent.Credits);
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Ship",
            shipSymbol
        });
    }

    [Route("/ships/{shipSymbol}/jumps/{jumpGate}")]
    public async Task<IActionResult> Jump(string shipSymbol, string jumpGate)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        await _shipsService.JumpAsync(jumpGate, shipSymbol);
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Ship",
            shipSymbol
        });
    }

    [Route("/ships/{shipSymbol}/sell/{inventorySymbol}")]
    public async Task<IActionResult> Sell(string shipSymbol, string inventorySymbol)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        var inventory = currentWaypoint.Marketplace.TradeGoods.Single(tg => tg.Symbol == inventorySymbol);
        var shipUnits = ship.Cargo.Inventory.Single(i => i.Symbol == inventorySymbol).Units;
        var sellAmount = Math.Min(inventory.TradeVolume, shipUnits);
        await _marketplacesService.SellAsync(shipSymbol, inventorySymbol, sellAmount);
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Ship",
            shipSymbol
        });
    }

    [Route("/ships/{shipSymbol}/transactions")]
    public async Task<IActionResult> Transactions(string shipSymbol)
    {
        var model = new ShipTransactionsModel(
            (await _shipStatusesCacheService.GetAsync(shipSymbol)).Ship,
            await _transactionsService.GetAsync(shipSymbol)
        );
        return View(model);
    }
}
