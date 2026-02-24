using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Shipyards;

public class ShipsService : IShipsService
{
    private const string DIRECTORY_PATH = "/v2/my/ships";
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<ShipsService> _logger;
    private readonly IWaypointsService _waypointsService;
    private readonly IShipStatusesCacheService _shipStatusesCacheService;
    private readonly IShipLogsService _shipLogsService;

    public ShipsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ShipsService> logger,
        IWaypointsService waypointsService,
        IShipStatusesCacheService shipStatusesCacheService,
        IShipLogsService shipLogsService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[$"SpaceTrader:" + ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[$"SpaceTrader:" + ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
        _waypointsService = waypointsService;
        _shipStatusesCacheService = shipStatusesCacheService;
        _shipLogsService = shipLogsService;
    }

    public async Task<IEnumerable<Ship>> GetAsync()
    {
        var page = 0;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var ships = new List<Ship>();
        Data<Ship> dataShip;
        do
        {
            page++;
            var url = new UriBuilder(_apiUrl)
            {
                Path = DIRECTORY_PATH,
                Query = $"limit=20&page={page}", // limit=20
            };
            dataShip = await HttpHelperService.HttpGetHelper<Data<Ship>>(
                url.ToString(),
                _httpClient,
                _logger);
            if (dataShip.DataList is null) throw new HttpRequestException("Ship not retrieved");
            ships.AddRange(dataShip.DataList);
            // return data.DataList;
        } while (dataShip.Meta.Limit * page < dataShip.Meta.Total);

        return ships;
    }

    public async Task<Ship> GetAsync(string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = DIRECTORY_PATH + $"/{shipSymbol}"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpGetHelper<DataSingle<Ship>>(
            url.ToString(),
            _httpClient,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        return data.Datum;
    }

    public async Task<Nav> OrbitAsync(string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/orbit"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
            url.ToString(),
            _httpClient,
            null,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Orbit Nav not retrieved");
        return data.Datum.Nav;
    }

    public async Task<Nav> DockAsync(string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/dock"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
            url.ToString(),
            _httpClient,
            null,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Dock Nav not retrieved");
        return data.Datum.Nav;
    }

    public async Task<(Nav, Fuel)> NavigateAsync(string waypointSymbol, Ship ship)
    {
        var waypoint = await _waypointsService.GetAsync(waypointSymbol);
        var currentWaypoint = await _waypointsService.GetAsync(ship.Nav.WaypointSymbol);
        return await NavigateAsync(waypoint, currentWaypoint, ship);
    }

    public async Task<(Nav, Fuel)> NavigateAsync(Waypoint waypoint, Waypoint currentWaypoint, Ship ship)
    {
        var distance = WaypointsService.CalculateDistance(waypoint.X, currentWaypoint.X, waypoint.Y, currentWaypoint.Y);
        if (distance > ship.Fuel.Current)
        {
            var flightMode = NavFlightModeEnum.DRIFT;
            await this.SwitchShipFlightMode(ship, flightMode);
        }
        else
        {
            var flightMode = NavFlightModeEnum.CRUISE;
            await this.SwitchShipFlightMode(ship, flightMode);
        }
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{ship.Symbol}/navigate"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { waypointSymbol = waypoint.Symbol });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data is null) throw new HttpRequestException("Nav error");
        await AddNavigateLog(ship, data.Datum.Nav, data.Datum.Fuel);
        return (data.Datum.Nav, data.Datum.Fuel);
    }

    private async Task AddFlightModeShipLog(string symbol, NavFlightModeEnum flightMode)
    {
        var datetime = DateTime.UtcNow;
        var shipLog = new ShipLog(
            symbol,
            ShipLogEnum.FlightMode,
            JsonSerializer.Serialize(new
            {
                FlightMode = flightMode.ToString(),
            }),
            datetime,
            datetime
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    private async Task AddNavigateLog(Ship ship, Nav nav, Fuel fuel)
    {
        var shipLog = new ShipLog(
            ship.Symbol,
            ShipLogEnum.Navigate,
            JsonSerializer.Serialize(new
            {
                OriginWaypointSymbol = nav.Route.Origin.Symbol,
                DestinationWaypointSymbol = nav.Route.Destination.Symbol,
                CurrentFuel = fuel.Current,
                OriginalFuel = fuel.Current + fuel.Consumed.Amount,
            }),
            nav.Route.DepartureTime,
            nav.Route.Arrival
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public async Task<(Nav, Cooldown)> JumpAsync(string waypointSymbol, string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/jump"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { waypointSymbol });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Jump Nav not retrieved");
        await AddJumpLog(shipSymbol, data.Datum.Nav, data.Datum.Cooldown);
        return (data.Datum.Nav, data.Datum.Cooldown);
    }

    private async Task AddJumpLog(string shipSymbol, Nav nav, Cooldown cooldown)
    {
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.Jump,
            JsonSerializer.Serialize(new
            {
                OriginWaypointSymbol = nav.Route.Origin.Symbol,
                DestinationWaypointSymbol = nav.Route.Destination.Symbol,
            }),
            nav.Route.DepartureTime,
            cooldown.Expiration
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public async Task<ExtractionResult> ExtractAsync(string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/extract"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<ExtractionResult>>(
            url.ToString(),
            _httpClient,
            null,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Jump Nav not retrieved");
        await AddExtractLog(shipSymbol, data.Datum.Cooldown, data.Datum.Extraction, data.Datum.Cargo);
        return data.Datum;
    }

    private async Task AddExtractLog(
        string shipSymbol, 
        Cooldown cooldown, 
        Extraction extraction, 
        Cargo cargo,
        Survey survey = null)
    {
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.Extract,
            JsonSerializer.Serialize(new
            {
                ExtractionYieldSymbol = extraction.Yield.Symbol,
                ExtractionYieldAmount = extraction.Yield.Units,
                CargoUnits = cargo.Units,
                CargoCapacity = cargo.Capacity,
                SurveySymbol = survey?.Symbol ?? "",
            }),
            cooldown.Expiration.AddSeconds(-cooldown.TotalSeconds),
            cooldown.Expiration
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public async Task<ExtractionResult> ExtractAsync(string shipSymbol, Survey survey)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/extract/survey"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(survey);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<ExtractionResult>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Survey not retrieved");
        await AddExtractLog(shipSymbol, data.Datum.Cooldown, data.Datum.Extraction, data.Datum.Cargo, survey);
        return data.Datum;
    }

    public async Task JettisonAsync(string shipSymbol, string inventorySymbol, int units)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/jettison"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { symbol = inventorySymbol, units });
        var response = await HttpHelperService.HttpPostHelper(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        await AddJettisonLog(shipSymbol, inventorySymbol, units);
        _logger.LogInformation("Data returned from Jettison: {response}", response);
    }

    private async Task AddJettisonLog(string shipSymbol, string inventorySymbol, int units)
    {
        var datetime = DateTime.UtcNow;
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.Jettison,
            JsonSerializer.Serialize(new
            {
                InventorySymbol = inventorySymbol,
                Units = units,
            }),
            datetime,
            datetime
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public static TimeSpan? GetShipCooldown(Ship ship)
    {
        if (ship.Nav.Route.Arrival > DateTime.UtcNow)
        {
            return ship.Nav.Route.Arrival - DateTime.UtcNow;
        }
        if (ship.Cooldown.Expiration > DateTime.UtcNow)
        {
            return ship.Cooldown.Expiration - DateTime.UtcNow;
        }
        return null;
    }

    public async Task<SurveyResult> SurveyAsync(string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/survey"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<SurveyResult>>(
            url.ToString(),
            _httpClient,
            null,
            _logger);
        if (data?.Datum is null) throw new HttpRequestException("Survey not retrieved");
        await AddSurveyShipLog(shipSymbol, data.Datum.Surveys, data.Datum.Cooldown);
        return data.Datum;
    }

    private async Task AddSurveyShipLog(string shipSymbol, IEnumerable<Survey> surveys, Cooldown cooldown)
    {
        var datetime = DateTime.UtcNow;
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.Survey,
            JsonSerializer.Serialize(new
            {
                Survey = string.Join(",", surveys.Select(s => s.Symbol))
            }),
            cooldown.Expiration.AddSeconds(-cooldown.TotalSeconds),
            cooldown.Expiration
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public async Task<ScanWaypointsResult> ScanWaypointsAsync(string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/scan/waypoints"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<ScanWaypointsResult>>(
            url.ToString(),
            _httpClient,
            null,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Scan not retrieved");
        return data.Datum;


        // var url = new UriBuilder(_apiUrl);
        // url.Path = $"v2/systems/{WaypointsService.ExtractSystemFromWaypoint(waypointSymbol)}/waypoints/{waypointSymbol}";
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", _token);
        // //HttpHelperService.HttpPostHelper();
        // var waypointsDataString = await _httpClient.GetAsync(url.ToString());
        // waypointsDataString.EnsureSuccessStatusCode();
        // var waypointsData = await waypointsDataString.Content.ReadFromJsonAsync<DataSingle<Waypoint>>();
        // if (waypointsData is null) throw new HttpRequestException("System Data not retrieved.");
        // if (waypointsData.Datum is null) throw new HttpRequestException("System not retrieved");
        // var waypoint = waypointsData.Datum;

        // Task<Marketplace?> marketplaceTask = Task.FromResult<Marketplace?>(null);
        // if (waypoint.Traits.Select(t => t.Symbol).Contains(WaypointTypesEnum.MARKETPLACE.ToString()))
        // {
        //     marketplaceTask = _marketplacesService.GetAsync(waypointSymbol);
        // }

        // Task<Shipyard?> shipyardTask = Task.FromResult<Shipyard?>(null);
        // if (waypoint.Traits.Select(t => t.Symbol).Contains(WaypointTypesEnum.SHIPYARD.ToString()))
        // {
        //     shipyardTask = _shipyardsService.GetAsync(waypointSymbol);
        // }

        // Task<JumpGate?> jumpGateTask = Task.FromResult<JumpGate?>(null);
        // if (waypoint.Type == WaypointTypesEnum.JUMP_GATE.ToString())
        // {
        //     jumpGateTask = _jumpGatesService.GetAsync(waypointSymbol);
        // }

        // Task<Construction?> constructionTask = Task.FromResult<Construction?>(null);
        // if (waypoint.IsUnderConstruction)
        // {
        //     constructionTask = _constructionsService.GetAsync(waypointSymbol);
        // }

        // waypoint = waypoint with
        // {
        //     Marketplace = await marketplaceTask,
        //     Shipyard = await shipyardTask,
        //     JumpGate = await jumpGateTask,
        //     Construction = await constructionTask
        // };
        // return waypoint;
    }

    public async Task<Nav> NavToggleAsync(string shipSymbol, NavFlightModeEnum flightMode)
    {
        var ship = await this.GetAsync(shipSymbol);
        if (ship.Nav.FlightMode != flightMode.ToString())
        {
            return await NavToggleAsync(ship, flightMode);
        }
        return ship.Nav;
    }

    private async Task<Nav> NavToggleAsync(Ship ship, NavFlightModeEnum flightMode)
    {
        var flightModeString = flightMode.ToString();
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/my/ships/{ship.Symbol}/nav"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { flightMode = flightModeString });
        var data = await HttpHelperService.HttpPatchHelper<DataSingle<NavToggleResult>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Nav Toggle not retrieved");
        await AddFlightModeShipLog(ship.Symbol, flightMode);
        return data.Datum.Nav;
    }

    public async Task<ChartWaypointResult> ChartAsync(string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/chart"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<ChartWaypointResult>>(
            url.ToString(),
            _httpClient,
            null,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Scan not retrieved");
        await AddChartShipLog(shipSymbol, data.Datum.Waypoint);
        return data.Datum;
    }

    private async Task AddChartShipLog(string shipSymbol, Waypoint waypoint)
    {
        var datetime = DateTime.UtcNow;
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.Chart,
            JsonSerializer.Serialize(new
            {
                Waypoint = waypoint.Symbol
            }),
            datetime,
            datetime
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public async Task<ScanSystemsResult> ScanSystemsAsync(string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/scan/systems"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var data = await HttpHelperService.HttpPostHelper<DataSingle<ScanSystemsResult>>(
            url.ToString(),
            _httpClient,
            null,
            _logger);
        if (data.Datum is null) throw new HttpRequestException("Scan not retrieved");
        return data.Datum;
    }

    public async Task SwitchShipFlightMode(Ship ship, NavFlightModeEnum flightMode)
    {
        if (ship.Nav.FlightMode == flightMode.ToString())
        {
            return;
        }
        var nav = await NavToggleAsync(ship.Symbol, flightMode);
        var shipStatuses = await _shipStatusesCacheService.GetAsync(ship.Symbol);
        shipStatuses = shipStatuses with { Ship = ship };
        await _shipStatusesCacheService.SetAsync(shipStatuses);
        await Task.Delay(500);
    }
}
