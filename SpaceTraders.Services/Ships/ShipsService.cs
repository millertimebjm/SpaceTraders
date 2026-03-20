using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceTraders.Dispatcher;
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Models.Results;
using SpaceTraders.Services.HttpHelpers;
using SpaceTraders.Services.ShipLogs.Interfaces;
using SpaceTraders.Services.Ships.Interfaces;
using SpaceTraders.Services.ShipStatuses.Interfaces;
using SpaceTraders.Services.Waypoints;
using SpaceTraders.Services.Waypoints.Interfaces;

namespace SpaceTraders.Services.Shipyards;

public class ShipsService(
    HttpClient _httpClient,
    IConfiguration _configuration,
    ILogger<ShipsService> _logger,
    IWaypointsService _waypointsService,
    IShipStatusesCacheService _shipStatusesCacheService,
    IShipLogsService _shipLogsService,
    IDispatcher _dispatcher
) : IShipsService
{
    private const string DIRECTORY_PATH = "/v2/my/ships";
 
    private string ApiUrl
    {
        get
        {
            var apiUrl = _configuration[$"SpaceTrader:" + ConfigurationEnums.ApiUrl.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl);
            return apiUrl;
        }
    }

    private string Token
    {
        get
        {
            var token = _configuration[$"SpaceTrader:" + ConfigurationEnums.AgentToken.ToString()] ?? string.Empty;
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            return token;
        }
    }

    public async Task<IEnumerable<Ship>> GetAsync()
    {
        var page = 0;
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Token);
        var ships = new List<Ship>();
        int total;

        do
        {
            page++;
            var url = new UriBuilder(ApiUrl)
            {
                Path = DIRECTORY_PATH,
                Query = $"limit=20&page={page}", // limit=20
            };
            // var dataShip = await HttpHelperService.HttpGetHelper<Data<Ship>>(
            //     url.ToString(),
            //     _httpClient,
            //     _logger);
            var request = new HttpRequestMessage(HttpMethod.Get, url.ToString());
            var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
            var dataShip = await response.Content.ReadFromJsonAsync<Data<Ship>>();
            if (dataShip.DataList is null) throw new HttpRequestException("Ship not retrieved");
            ships.AddRange(dataShip.DataList);
            total = dataShip.Meta.Total;
        } while (ships.Count() < total);

        return ships;
    }

    public async Task<Ship> GetAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = DIRECTORY_PATH + $"/{shipSymbol}"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpGetHelper<DataSingle<Ship>>(
        //     url,
        //     _httpClient,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Ship not retrieved");
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Ship not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<Ship>>();
        return data.Datum;
    }

    public async Task<Nav> OrbitAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/orbit"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
        //     url,
        //     _httpClient,
        //     null,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Orbit Nav not retrieved");
        // return data.Datum.Nav;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Nav not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<Ship>>();
        return data.Datum.Nav;
    }

    public async Task<Nav> DockAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/dock"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
        //     url.ToString(),
        //     _httpClient,
        //     null,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Dock Nav not retrieved");
        // return data.Datum.Nav;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Nav not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<Ship>>();
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
        var distance = WaypointsService.CalculateDistance(waypoint.X, waypoint.Y, currentWaypoint.X, currentWaypoint.Y);
        if (distance > ship.Fuel.Current || ship.Fuel.Current == 0)
        {
            var flightMode = NavFlightModeEnum.DRIFT;
            await NavToggleAsync(ship, flightMode);
        }
        else
        {
            var flightMode = NavFlightModeEnum.CRUISE;
            await NavToggleAsync(ship, flightMode);
        }
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{ship.Symbol}/navigate"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { waypointSymbol = waypoint.Symbol });
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<Ship>>(
        //     url,
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data is null) 
        //     throw new HttpRequestException("Nav error");
        // await AddNavigateLog(ship, data.Datum.Nav, data.Datum.Fuel);
        // return (data.Datum.Nav, data.Datum.Fuel);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { waypointSymbol = waypoint.Symbol });
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Ship not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<Ship>>();
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
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/jump"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { waypointSymbol });
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<JumpResponse>>(
        //     url,
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Jump Nav not retrieved");
        // await AddJumpLog(shipSymbol, data.Datum.Nav, data.Datum.Cooldown, data.Datum.Transaction);
        // return (data.Datum.Nav, data.Datum.Cooldown);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { waypointSymbol });
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Ship not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<JumpResponse>>();
        await AddJumpLog(shipSymbol, data.Datum.Nav, data.Datum.Cooldown, data.Datum.Transaction);
        return (data.Datum.Nav, data.Datum.Cooldown);
    }

    private async Task AddJumpLog(string shipSymbol, Nav nav, Cooldown cooldown, MarketTransaction transaction)
    {
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.Jump,
            JsonSerializer.Serialize(new
            {
                OriginWaypointSymbol = nav.Route.Origin.Symbol,
                DestinationWaypointSymbol = nav.Route.Destination.Symbol,
                InventorySymbol = transaction.TradeSymbol,
                InventoryUnits = transaction.Units,
                CreditsPerUnit = transaction.PricePerUnit,
                TotalCredits = transaction.TotalPrice,
            }),
            nav.Route.DepartureTime,
            cooldown.Expiration
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public async Task<ExtractionResult> ExtractAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/extract"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<ExtractionResult>>(
        //     url,
        //     _httpClient,
        //     null,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Extract not retrieved");
        // await AddExtractLog(shipSymbol, data.Datum.Cooldown, data.Datum.Extraction, data.Datum.Cargo);
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Extraction not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<ExtractionResult>>();
        await AddExtractLog(shipSymbol, data.Datum.Cooldown, data.Datum.Extraction, data.Datum.Cargo);
        return data.Datum;
    }

    public async Task<SiphonResult> SiphonAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/siphon"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<SiphonResult>>(
        //     url,
        //     _httpClient,
        //     null,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Siphon not retrieved");
        // await AddSiphonLog(shipSymbol, data.Datum.Cooldown, data.Datum.Siphon, data.Datum.Cargo);
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Siphon not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<SiphonResult>>();
        await AddSiphonLog(shipSymbol, data.Datum.Cooldown, data.Datum.Siphon, data.Datum.Cargo);
        return data.Datum;
    }

    private async Task AddSiphonLog(
        string shipSymbol, 
        Cooldown cooldown, 
        Siphon siphon, 
        Cargo cargo)
    {
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.Siphon,
            JsonSerializer.Serialize(new
            {
                SiphonYieldSymbol = siphon.Yield.Symbol,
                SiphonYieldAmount = siphon.Yield.Units,
                CargoUnits = cargo.Units,
                CargoCapacity = cargo.Capacity,
            }),
            cooldown.Expiration.AddSeconds(-cooldown.TotalSeconds),
            cooldown.Expiration
        );
        await _shipLogsService.AddAsync(shipLog);
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
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/extract/survey"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(survey);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<ExtractionResult>>(
        //     url,
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Survey not retrieved");
        // await AddExtractLog(shipSymbol, data.Datum.Cooldown, data.Datum.Extraction, data.Datum.Cargo, survey);
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(survey);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Survey not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<ExtractionResult>>();
        await AddExtractLog(shipSymbol, data.Datum.Cooldown, data.Datum.Extraction, data.Datum.Cargo, survey);
        return data.Datum;
    }

    public async Task<Cargo> JettisonAsync(string shipSymbol, string inventorySymbol, int units)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/jettison"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { symbol = inventorySymbol, units });
        // var response = await HttpHelperService.HttpPostHelper<DataSingle<Cargo>>(
        //     url,
        //     _httpClient,
        //     content,
        //     _logger);
        // await AddJettisonLog(shipSymbol, inventorySymbol, units);
        // _logger.LogInformation("Data returned from Jettison: {response}", response);
        // return response.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { symbol = inventorySymbol, units });
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Jettison not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<Cargo>>();
        await AddJettisonLog(shipSymbol, inventorySymbol, units);
        _logger.LogInformation("Data returned from Jettison: {response}", response);
        return data.Datum;
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
        if (ship.Cooldown?.Expiration > DateTime.UtcNow)
        {
            return ship.Cooldown.Expiration - DateTime.UtcNow;
        }
        return null;
    }

    public async Task<SurveyResult> SurveyAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/v2/my/ships/{shipSymbol}/survey"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<SurveyResult>>(
        //     url.ToString(),
        //     _httpClient,
        //     null,
        //     _logger);
        // if (data?.Datum is null) throw new HttpRequestException("Survey not retrieved");
        // await AddSurveyShipLog(shipSymbol, data.Datum.Surveys, data.Datum.Cooldown);
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Survey not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<SurveyResult>>();
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
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/scan/waypoints"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<ScanWaypointsResult>>(
        //     url,
        //     _httpClient,
        //     null,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Scan not retrieved");
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        // var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Scan not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<ScanWaypointsResult>>();
        return data.Datum;
    }

    public async Task<Nav> NavToggleAsync(Ship ship, NavFlightModeEnum flightMode)
    {
        if (ship.Nav.FlightMode == flightMode.ToString()) return ship.Nav;

        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/my/ships/{ship.Symbol}/nav"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { flightMode = flightModeString });
        // var data = await HttpHelperService.HttpPatchHelper<DataSingle<NavToggleResult>>(
        //     url,
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Nav Toggle not retrieved");
        // await AddFlightModeShipLog(ship.Symbol, flightMode);
        // return data.Datum.Nav;

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { flightMode = flightMode.ToString() });
        // var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Nav Toggle not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<NavToggleResult>>();
        await AddFlightModeShipLog(ship.Symbol, flightMode);
        return data.Datum.Nav;
    }

    public async Task<ChartWaypointResult> ChartAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/chart"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<ChartWaypointResult>>(
        //     url,
        //     _httpClient,
        //     null,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Scan not retrieved");
        // await AddChartShipLog(shipSymbol, data.Datum.Waypoint);
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Scan not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<ChartWaypointResult>>();
        await AddChartShipLog(shipSymbol, data.Datum.Waypoint, data.Datum.Transaction);
        return data.Datum;
    }

    private async Task AddChartShipLog(string shipSymbol, Waypoint waypoint, MarketTransaction transaction)
    {
        var datetime = DateTime.UtcNow;
        var shipLog = new ShipLog(
            shipSymbol,
            ShipLogEnum.Chart,
            JsonSerializer.Serialize(new
            {
                Waypoint = waypoint.Symbol,
                TotalCredits = transaction.TotalPrice,
            }),
            datetime,
            datetime
        );
        await _shipLogsService.AddAsync(shipLog);
    }

    public async Task<ScanSystemsResult> ScanSystemsAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/scan/systems"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<ScanSystemsResult>>(
        //     url,
        //     _httpClient,
        //     null,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Scan not retrieved");
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Scan not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<ScanSystemsResult>>();
        return data.Datum;
    }

    public async Task<ScanSystemsResult> ScrapAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/scrap"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<ScanSystemsResult>>(
        //     url,
        //     _httpClient,
        //     null,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Scrap not retrieved");
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Scan not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<ScanSystemsResult>>();
        return data.Datum;
    }

    // public static async Task<IEnumerable<Ship>> HexadecimalSort(this IEnumerable<Ship> ships)
    // {
    //     return ships.OrderBy(s => {
    //         var parts = s.Symbol.Split('-');
    //         return Convert.ToInt32(parts[1], 16); // Parse as hex
    //     });
    // }

    public async Task<TransferCargoResult> TransferCargo(string shipSymbol, string targetShipSymbol, string inventorySymbol, int inventoryAmount)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/transfer"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { tradeSymbol = inventorySymbol, units = inventoryAmount, shipSymbol = targetShipSymbol });
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<TransferCargoResult>>(
        //     url,
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Transfer not retrieved");
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { tradeSymbol = inventorySymbol, units = inventoryAmount, shipSymbol = targetShipSymbol });
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Transfer not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<TransferCargoResult>>();
        return data.Datum;
    }

    public async Task<ScrapShipResponse> ScrapShipAsync(string shipSymbol)
    {
        var urlBuilder = new UriBuilder(ApiUrl)
        {
            Path = $"/my/ships/{shipSymbol}/scrap"
        };
        var url = urlBuilder.ToString();
        // _httpClient.DefaultRequestHeaders.Authorization =
        //     new AuthenticationHeaderValue("Bearer", Token);
        // var content = JsonContent.Create(new { tradeSymbol = inventorySymbol, units = inventoryAmount, shipSymbol = targetShipSymbol });
        // var data = await HttpHelperService.HttpPostHelper<DataSingle<TransferCargoResult>>(
        //     url,
        //     _httpClient,
        //     content,
        //     _logger);
        // if (data.Datum is null) throw new HttpRequestException("Scrap not retrieved");
        // return data.Datum;

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        //var response = await _dispatcher.SendAsync(request);
        //var response = await _httpClient.SendAsync(request);
        var response = await HttpHelperService.HttpSendHelper(_httpClient, request, _logger);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("Scrap not retrieved");
        var data = await response.Content.ReadFromJsonAsync<DataSingle<ScrapShipResponse>>();
        return data.Datum;
    }
}
