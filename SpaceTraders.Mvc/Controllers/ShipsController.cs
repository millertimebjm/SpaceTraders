using Microsoft.AspNetCore.Mvc;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Models.Results;
using SpaceTraders.Mvc.Models;
using SpaceTraders.Services.Agents.Interfaces;
using SpaceTraders.Services.Contracts.Interfaces;
using SpaceTraders.Services.Marketplaces.Interfaces;
using SpaceTraders.Services.Ships;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Shipyards;
using SpaceTraders.Services.Surveys.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Transactions.Interfaces;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Mvc.Controllers;

public class ShipsController(
    IShipsService _shipsService,
    IWaypointsService _waypointsService,
    IMarketplacesService _marketplacesService,
    IAgentsService _agentsService,
    IContractsService _contractsService,
    ISurveysCacheService _surveysCacheService,
    ITransactionsCacheService _transactionsService,
    IShipStatusesCacheService _shipStatusesCacheService,
    ISystemsService _systemsService
) : BaseController(_agentsService, _shipStatusesCacheService, _systemsService)
{
    [Route("/ships")]
    public async Task<IActionResult> Index()
    {
        var shipStatuses = await _shipStatusesCacheService.GetAsync();
        var ships = shipStatuses.Select(ss => ss.Ship).HexadecimalSort();
        var systems = await _systemsService.GetAsync();
        IReadOnlyList<Waypoint> waypoints = systems.SelectMany(s => s.Waypoints).ToList();
        ShipsViewModel model = new(
            Task.FromResult(ships),
            //_contractsService.GetActiveAsync());
            Task.FromResult((STContract?)null),
            Task.FromResult(waypoints));
        return View(model);
    }

    [Route("/ships/{shipSymbol}/active")]
    public async Task<IActionResult> SetActive(string shipSymbol)
    {
        var shipsStatus = await _shipStatusesCacheService.GetAsync();
        var ships = shipsStatus.Select(ss => ss.Ship);
        var ship = ships.Single(s => s.Symbol == shipSymbol);
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

        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipStatus.Ship;
        ship = ship with { Nav = nav };
        shipStatus = shipStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatus);

        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/{shipSymbol}/dock")]
    public async Task<IActionResult> Dock(string shipSymbol)
    {
        var nav = await _shipsService.DockAsync(shipSymbol);

        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipStatus.Ship;
        ship = ship with { Nav = nav };
        shipStatus = shipStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatus);

        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/extract")]
    public async Task<IActionResult> Extract()
    {
        var shipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        var extractResult = await _shipsService.ExtractAsync(shipSymbol);

        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipStatus.Ship;
        ship = ship with { Cargo = extractResult.Cargo, Cooldown = extractResult.Cooldown};
        shipStatus = shipStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatus);

        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/siphon")]
    public async Task<IActionResult> Siphon()
    {
        var shipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(shipSymbol);
        var siphonResult = await _shipsService.SiphonAsync(shipSymbol);
        
        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipStatus.Ship;
        ship = ship with { Cargo = siphonResult.Cargo, Cooldown = siphonResult.Cooldown};
        shipStatus = shipStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatus);

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
            await _surveysCacheService.SetAsync(survey);
        }
        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/{shipSymbol}")]
    public async Task<IActionResult> Ship(string shipSymbol)
    {
        var shipsStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipsStatus.Ship;
        ShipViewModel model = new(
            Task.FromResult(ship),
            _contractsService.GetActiveAsync(),
            _waypointsService.GetAsync(ship.Nav.WaypointSymbol));
        return View(model);
    }

    [Route("/ships/{shipSymbol}/reset")]
    public async Task<IActionResult> Reset(string shipSymbol)
    {
        var shipsStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = await _shipsService.GetAsync(shipSymbol);
        shipsStatus = shipsStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipsStatus);
        return Redirect($"/ships/{shipSymbol}");
    }

    [Route("/ships/{shipSymbol}/jettison/{inventorySymbol}")]
    public async Task<IActionResult> Jettison(string shipSymbol, string inventorySymbol)
    {
        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipStatus.Ship;
        var cargo = await _shipsService.JettisonAsync(shipSymbol, inventorySymbol, ship.Cargo.Inventory.Single(i => i.Symbol == inventorySymbol).Units);
        ship = ship with { Cargo = cargo };
        shipStatus = shipStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatus);
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
        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipStatus.Ship;
        var refuelResponse = await _marketplacesService.RefuelAsync(shipSymbol);
        ship = ship with { Fuel = refuelResponse.Fuel };
        shipStatus = shipStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatus);
        
        SessionHelper.Set(HttpContext, SessionEnum.CurrentCredits, refuelResponse.Agent.Credits);
        await _agentsService.SetAsync(refuelResponse.Agent);

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
        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipStatus.Ship;
        var (nav, cooldown) = await _shipsService.JumpAsync(jumpGate, shipSymbol);
        ship = ship with {Cooldown = cooldown, Nav = nav };
        shipStatus = shipStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatus);

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
        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipStatus.Ship;
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        var inventory = currentWaypoint.Marketplace!.TradeGoods!.Single(tg => tg.Symbol == inventorySymbol);
        var shipUnits = ship.Cargo.Inventory.Single(i => i.Symbol == inventorySymbol).Units;
        var sellAmount = Math.Min(inventory.TradeVolume, shipUnits);
        var sellCargoResponse = await _marketplacesService.SellAsync(shipSymbol, inventorySymbol, sellAmount);
        ship = ship with { Cargo = sellCargoResponse.Cargo };
        shipStatus = shipStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatus);
        await _agentsService.SetAsync(sellCargoResponse.Agent);
        SessionHelper.Set(HttpContext, SessionEnum.CurrentCredits, sellCargoResponse.Agent.Credits);

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

    [Route("/ships/{shipSymbol}/scanwaypoints")]
    public async Task<IActionResult> ScanWaypoints(string shipSymbol)
    {
        var result = _shipsService.ScanWaypointsAsync(shipSymbol);
        return View(result);
    }

    [Route("/ships/{shipSymbol}/scansystems")]
    public async Task<IActionResult> ScanSystems(string shipSymbol)
    {
        var result = _shipsService.ScanSystemsAsync(shipSymbol);
        return View(result);
    }

    [Route("/ships/{shipSymbol}/navtoggle")]
    public async Task<IActionResult> NavToggle(string shipSymbol, string flightMode)
    {
        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        var ship = shipStatus.Ship;
        Nav nav;
        if (ship.Nav.FlightMode != NavFlightModeEnum.CRUISE.ToString())
        {
            nav = await _shipsService.NavToggleAsync(ship, Enum.Parse<NavFlightModeEnum>(flightMode));
            ship = ship with { Nav = nav };
            shipStatus = shipStatus with { Ship = ship };
            await _shipStatusesCacheService.SetAsync(shipStatus);
        }
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Ship",
            shipSymbol
        });
    }

    [Route("/ships/{shipSymbol}/refresh")]
    public async Task<IActionResult> Refresh(string shipSymbol)
    {
        var ship = await _shipsService.GetAsync(shipSymbol);
        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        shipStatus = shipStatus with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatus);

        var currentShipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
        if (currentShipSymbol == shipSymbol)
        {
            SessionHelper.Set(HttpContext, SessionEnum.CurrentWaypointSymbol, ship.Nav.WaypointSymbol);
        }
        
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Ship",
            shipSymbol
        });
    }

    [Route("/ships/reset")]
    public async Task<IActionResult> Reset()
    {
        var ships = await _shipsService.GetAsync();
        var shipStatuses = await _shipStatusesCacheService.GetAsync();
        foreach (var ship in ships)
        {
            var shipStatus = shipStatuses.SingleOrDefault(ss => ss.Ship.Symbol == ship.Symbol);
            if (shipStatus is not null)
            {
                var newShip = ship with { Cargo = ship.Cargo, Fuel = ship.Fuel };
                shipStatus = shipStatus with { Ship = ship };
            }
            else
            {
                shipStatus = new ShipStatus(ship, "New ship", DateTime.UtcNow);
            }
            await _shipStatusesCacheService.SetAsync(shipStatus);

            var currentShipSymbol = SessionHelper.Get<string>(HttpContext, SessionEnum.CurrentShipSymbol);
            if (currentShipSymbol == ship.Symbol)
            {
                SessionHelper.Set(HttpContext, SessionEnum.CurrentWaypointSymbol, ship.Nav.WaypointSymbol);
            }
        }
        
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Index",
        });
    }

    [Route("/ships/{shipSymbol}/scrap")]
    public async Task<IActionResult> Scrap(string shipSymbol)
    {
        var ships = await _shipsService.GetAsync();
        var shipStatuses = await _shipStatusesCacheService.GetAsync();
        foreach (var ship in ships)
        {
            var shipStatus = shipStatuses.SingleOrDefault(ss => ss.Ship.Symbol == ship.Symbol);
            if (shipStatus is not null)
            {
                shipStatus = shipStatus with { Ship = ship };
            }
            else
            {
                shipStatus = new ShipStatus(ship, "New ship", DateTime.UtcNow);
            }
            await _shipStatusesCacheService.SetAsync(shipStatus);

            SessionHelper.Unset(HttpContext, SessionEnum.CurrentWaypointSymbol);
        }
        
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Index",
        });
    }

    [Route("/ships/{shipSymbol}/remove")]
    public async Task<IActionResult> Remove(string shipSymbol)
    {
        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        await _shipStatusesCacheService.DeleteAsync(shipStatus);
        
        return RedirectToRoute(new
        {
            controller = "Ships",
            action = "Index",
        });
    }

    [Route("/ships/{shipSymbol}/detail")]
    public async Task<IActionResult> Detail(string shipSymbol)
    {
        var shipStatus = await _shipStatusesCacheService.GetAsync(shipSymbol);
        return Json(shipStatus.Ship);
    }
}
