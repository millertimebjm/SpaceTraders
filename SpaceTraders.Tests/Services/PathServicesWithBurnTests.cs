using System.Diagnostics;
using NSubstitute;
using SpaceTraders.Models;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Tests;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
public class PathsServicesWithBurnTests
{
    [Fact]
    public async Task WaypointPath_LongPath()
    {
        var systemSymbol = "X1-X1";
        var originSymbol = $"{systemSymbol}-origin";
        var destinationSymbol = $"{systemSymbol}-destination";
        var waypoints = new List<Waypoint>()
        {
            new(originSymbol, systemSymbol, "", 0, 0, null, "", null, null, null, null, false, null, null),
            new(destinationSymbol, systemSymbol, "", 400, 400, null, "", null, null, null, null, false, null, null),
        };
        //var system = new STSystem("", "X1-X1", "X1", null, 0, 0, [.. waypoints], null, "");
        // IPathsCacheService pathsCacheService = Substitute.For<IPathsCacheService>();
        // ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        // systemsServiceSub
        //     .GetAsync()
        //     .Returns([system]);
        // var pathsService = new PathsService(systemsServiceSub, pathsCacheService);
        var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints, waypoints.First().Symbol, 1, 1);
        var path = paths.Single(p => 
            p.WaypointSymbol == waypoints.Last().Symbol 
            && waypoints.Last().Symbol == destinationSymbol
            && waypoints.First().Symbol == originSymbol);
        Assert.True(path.TimeCost > 4000);
    }

    [Fact]
    public async Task WaypointPath_EasyPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("X1-X1-origin", "X1-X1", "", 0, 0, null, "", null, null, null, null, false, null, null),
            new("X1-X1-destination", "X1-X1", "", 1, 1, null, "", null, null, null, null, false, null, null),
        };
        // var system = new STSystem("", "X1-X1", "X1", null, 0, 0, waypoints.ToList(), null, "");

        // IPathsCacheService pathsCacheService = Substitute.For<IPathsCacheService>();
        // ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        // systemsServiceSub
        //     .GetAsync()
        //     .Returns([system]);
        // var pathsService = new PathsService(systemsServiceSub, pathsCacheService);
        var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints, waypoints.First().Symbol, 2, 2);
        var path = paths.Single(p => p.WaypointSymbol == waypoints.Last().Symbol);
        Assert.Equal(path.PathWaypoints.First().WaypointSymbol, waypoints.First().Symbol);
        Assert.Equal(path.PathWaypoints.Last().WaypointSymbol, waypoints.Last().Symbol);
        Assert.True(path.TimeCost < 3);
    }

    [Fact]
    public async Task WaypointPath_RefuelPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("X1-X1-origin", "", "", 0, 0, null, "", null, null, null, null, false, null, null),
            new("X1-X1-refuel", "", "", 0, 100, null, "", null, null, new Marketplace("X1-X1-refuel", null, null, [new("FUEL", "FUEL", "FUEL")], null, null), null, false, null, null),
            new("X1-X1-destination", "", "", 0, 200, null, "", null, null, null, null, false, null, null),
        };
        // var system = new STSystem("", "X1-X1-", "", "", 0, 0, null, null, null);
        // IPathsCacheService pathsCacheService = Substitute.For<IPathsCacheService>();
        // ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        // systemsServiceSub
        //     .GetAsync()
        //     .Returns([system]);
        // var pathsService = new PathsService(systemsServiceSub, pathsCacheService);
        var firstWaypointSymbol = waypoints.First().Symbol;
        var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints, firstWaypointSymbol, 105, 105);
        var path = paths.Single(p => p.WaypointSymbol == "X1-X1-destination");
        Assert.Equal(waypoints[0].Symbol, path.PathWaypoints[0].WaypointSymbol);
        Assert.Equal(waypoints[1].Symbol, path.PathWaypoints[1].WaypointSymbol);
        Assert.Equal(waypoints[2].Symbol, path.PathWaypoints[2].WaypointSymbol);
        Assert.Equal(waypoints.Count(), path.PathWaypoints.Count());

        Assert.Equal(path.PathWaypoints.First().WaypointSymbol, waypoints.First().Symbol);
        Assert.Equal(path.PathWaypoints.Last().WaypointSymbol, waypoints.Last().Symbol);
    }

    [Fact]
    public async Task WaypointPath_PartialDrift()
    {
        var waypoints = new List<Waypoint>()
        {
            new("X1-X1-origin", "", "", 0, 0, null, "", null, null, null, null, false, null, null),
            new("X1-X1-refuel", "", "", 0, 100, null, "", null, null, new Marketplace("X1-X1-refuel", null, null, [new("FUEL", "FUEL", "FUEL")], null, null), null, false, null, null),
            new("X1-X1-destination", "", "", 0, 180, null, "", null, null, null, null, false, null, null),
        };
        // var system = new STSystem("", "X1-X1-", "", "", 0, 0, null, null, null);
        // IPathsCacheService pathsCacheService = Substitute.For<IPathsCacheService>();
        // ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        // systemsServiceSub
        //     .GetAsync()
        //     .Returns([system]);
        // var pathsService = new PathsService(systemsServiceSub, pathsCacheService);
        var paths = PathsService.BuildSystemPathWithCostWithBurn(waypoints, waypoints.First().Symbol, 90, 90);
        var path = paths.Single(p => p.WaypointSymbol == "X1-X1-destination");
        Assert.Equal(waypoints[0].Symbol, path.PathWaypoints[0].WaypointSymbol);
        Assert.Equal(waypoints[1].Symbol, path.PathWaypoints[1].WaypointSymbol);
        Assert.Equal(waypoints[2].Symbol, path.PathWaypoints[2].WaypointSymbol);
        Assert.Equal(waypoints.Count(), path.PathWaypoints.Count());

        Assert.Equal(path.PathWaypoints.First().WaypointSymbol, waypoints.First().Symbol);
        Assert.Equal(path.PathWaypoints.Last().WaypointSymbol, waypoints.Last().Symbol);
    }

    // [Fact]
    // public async Task SystemPath_EasyPath()
    // {
    //     var firstWaypointSymbol = "A1-AB1-ABC1";
    //     var secondWaypointSymbol = "Z1-ZY1-ZYX1";
    //     var secondWaypoint = new Waypoint(secondWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", 0, 0, null, "", null, null, null, JumpGate: new JumpGate(secondWaypointSymbol, [firstWaypointSymbol]), IsUnderConstruction: false, null, null);
    //     var secondSystem = new STSystem("", WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", "", 0, 0, new List<Waypoint> { secondWaypoint }, null, null);

    //     var firstWaypointCompletedJumpGate = new Waypoint(firstWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(firstWaypointSymbol), "", 0, 0, null, "", null, null, null, new JumpGate(firstWaypointSymbol, [secondWaypointSymbol]), IsUnderConstruction: false, null, null);
    //     var firstSystem = new STSystem("Constellation", "A1-AB1", "A1", "", 0, 0, new List<Waypoint> { firstWaypointCompletedJumpGate }, null, "");

    //     IPathsCacheService pathsCacheService = Substitute.For<IPathsCacheService>();
    //     ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
    //     systemsServiceSub
    //         .GetAsync()
    //         .Returns(new List<STSystem> { firstSystem, secondSystem });

    //     var ship = new Ship("", null, null, null, null, null, null, null, null, null, new Fuel(10, 10, null), null, null, null);

    //     IPathsService pathsService = new PathsService(systemsServiceSub, pathsCacheService);
    //     var systemPath = await PathsService.BuildSystemPathWithCostWithBurn(
    //         firstWaypointSymbol,
    //         ship.Fuel.Capacity, ship.Fuel.Current);
    //     Assert.Equal(2, systemPath.Single(p => p.WaypointSymbol == secondWaypointSymbol).PathWaypointSymbols.Count());
    //     Assert.Equal(firstWaypointSymbol, systemPath.Single(p => p.WaypointSymbol == secondWaypointSymbol).PathWaypointSymbols[0]);
    //     Assert.Equal(secondWaypointSymbol, systemPath.Single(p => p.WaypointSymbol == secondWaypointSymbol).PathWaypointSymbols[1]);
    // }
}
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.