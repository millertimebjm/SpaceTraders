using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.Ships.Interfaces;

namespace SpaceTraders.Services.Shipyards;

public class ShipsService : IShipsService
{
    private const string DIRECTORY_PATH = "/v2/my/ships";
    private readonly string _apiUrl;
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly ILogger<ShipsService> _logger;
    private readonly IMongoCollectionFactory _collectionFactory;

    public ShipsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ShipsService> logger,
        IMongoCollectionFactory collectionFactory)
    {
        _collectionFactory = collectionFactory;
        _logger = logger;
        _httpClient = httpClient;
        _apiUrl = configuration[$"SpaceTrader:" + ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_apiUrl);
        _token = configuration[$"SpaceTrader:" + ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
        ArgumentException.ThrowIfNullOrWhiteSpace(_token);
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

    public async Task<(Nav, Fuel)> NavigateAsync(string waypointSymbol, string shipSymbol)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/navigate"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { waypointSymbol });
        var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        if (data is null) throw new HttpRequestException("Nav error");
        return (data.Datum.Nav, data.Datum.Fuel);
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
        return (data.Datum.Nav, data.Datum.Cooldown);
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
        return data.Datum;
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
        _logger.LogInformation("Data returned from Jettison: {response}", response);
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
        if (data is null) throw new HttpRequestException("Survey not retrieved");
        if (data.Datum is null) throw new HttpRequestException("Survey not retrieved");
        return data.Datum;
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

    public async Task NavToggleAsync(string shipSymbol, string flightMode)
    {
        var url = new UriBuilder(_apiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/nav"
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        var content = JsonContent.Create(new { flightMode });
        await HttpHelperService.HttpPatchHelper(
            url.ToString(),
            _httpClient,
            content,
            _logger);
        // if (data.Datum is null) throw new HttpRequestException("Scan not retrieved");
        // return data.Datum;
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
        return data.Datum;
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
        await NavToggleAsync(ship.Symbol, flightMode.ToString());
        await Task.Delay(500);
    }
}
