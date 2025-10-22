using Moq;
using SpaceTraders.Models;
using SpaceTraders.Services.IoWrappers.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Tests;

public class WaypointsFileCacheServiceTests
{
    private readonly WaypointsFileCacheService _service;
    private const string _waypointSymbol = "A1-B2-CD3";
    private const string _systemSymbol = "A1-B2";
    private const string _type = "Type1";
    private const string _traitSymbol = "TraitSymbol1";
    private readonly Trait _trait = new Trait(_traitSymbol, "TraitName1", "TraitDescription1", _waypointSymbol);
    private static readonly IReadOnlyList<Trait> _traits = new List<Trait>()
    {
        new Trait(_traitSymbol, "", "", ""),
        new Trait("Other Trait Symbol", "", "", ""),
    };
    private readonly Waypoint _waypoint = new Waypoint(
        Symbol: _waypointSymbol,
        SystemSymbol: _systemSymbol,
        Type: _type,
        X: 0,
        Y: 0,
        Orbitals: null,
        Orbits: "",
        Traits: _traits,
        Shipyard: null,
        Marketplace: null,
        JumpGate: null,
        IsUnderConstruction: false,
        Construction: null); // Construction
    
    public WaypointsFileCacheServiceTests()
    {
        var fileWrapperMock = new Mock<IFileWrapper>();
        _service = new WaypointsFileCacheService(fileWrapperMock.Object);
        var task = _service.SetAsync(_waypoint);
        task.Wait();
    }


    [Fact]
    public async Task Find_Single()
    {
        var waypointInDatabase = await _service.GetAsync(_waypointSymbol);
        Assert.NotNull(waypointInDatabase);
        Assert.Equal(_waypointSymbol, waypointInDatabase.Symbol);
    }

    [Fact]
    public async Task GetAsync_Null()
    {
        var waypointInDatabase = await _service.GetAsync("Not In Database");
        Assert.Null(waypointInDatabase);
    }

    [Fact]
    public async Task GetByTypeAsync_Single()
    {
        var waypointsInDatabase = await _service.GetByTypeAsync(_systemSymbol, _type);
        Assert.NotNull(waypointsInDatabase);
        Assert.Single(waypointsInDatabase);
        Assert.Equal(_waypointSymbol, waypointsInDatabase.Single().Symbol);
        Assert.Equal(_systemSymbol, waypointsInDatabase.Single().SystemSymbol);
        Assert.Equal(_type, waypointsInDatabase.Single().Type);
    }

    [Fact]
    public async Task GetByTypeAsync_Empty()
    {
        var waypointsInDatabase = await _service.GetByTypeAsync(_systemSymbol, "Not In Database");
        Assert.NotNull(waypointsInDatabase);
        Assert.Empty(waypointsInDatabase);
    }

    [Fact]
    public async Task GetByTraitAsync_Single()
    {
        var waypointsInDatabase = await _service.GetByTraitAsync(_systemSymbol, _traitSymbol);
        Assert.NotNull(waypointsInDatabase);
        Assert.Single(waypointsInDatabase);
        Assert.Contains(waypointsInDatabase.Single().Traits, t => t.Symbol == _traitSymbol);
        Assert.Equal(_waypointSymbol, waypointsInDatabase.Single().Symbol);
        Assert.Equal(_systemSymbol, waypointsInDatabase.Single().SystemSymbol);
    }

    [Fact]
    public async Task GetByTraitAsync_Empty()
    {
        var waypointsInDatabase = await _service.GetByTraitAsync(_systemSymbol, "Not In Database");
        Assert.NotNull(waypointsInDatabase);
        Assert.Empty(waypointsInDatabase);
    }

    [Fact]
    public async Task SetAsync_Single()
    {
        var newWaypointSymbol = "NewWaypointSymbol";
        var newWaypoint = new Waypoint(newWaypointSymbol, "", "", 0, 0, null, "", null, null, null, null, false, null);
        await _service.SetAsync(newWaypoint);
        var newWaypointInDatabase = await _service.GetAsync(newWaypointSymbol);
        Assert.NotNull(newWaypointInDatabase);
        Assert.Equal(newWaypoint.Symbol, newWaypointInDatabase.Symbol);
    }
}