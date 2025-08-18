using NSubstitute;
using SpaceTraders.Models;
using SpaceTraders.Services.Paths;
using SpaceTraders.Services.Paths.Interfaces;
using SpaceTraders.Services.Systems.Interfaces;
using SpaceTraders.Services.Waypoints;

namespace SpaceTraders.Tests;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
public class PathsServicesTests
{
    [Fact]
    public void WaypointPath_NoPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("origin", "", "", 0, 0, null, "", null, null, null, null, false, null),
            new("destination", "", "", 400, 400, null, "", null, null, null, null, false, null),
        };
        var paths = PathsService.BuildWaypointPath(waypoints, waypoints.First(), 1, 1);
        var path = paths.SingleOrDefault(p => p.Key.Symbol == waypoints.Last().Symbol);
        Assert.Equal(default, path);
    }

    [Fact]
    public void WaypointPath_EasyPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("origin", "", "", 0, 0, null, "", null, null, null, null, false, null),
            new("destination", "", "", 1, 1, null, "", null, null, null, null, false, null),
        };
        var paths = PathsService.BuildWaypointPath(waypoints, waypoints.First(), 2, 2);
        var path = paths.SingleOrDefault(p => p.Key.Symbol == waypoints.Last().Symbol);
        Assert.NotEqual(default, path);
        Assert.Equal(path.Value.Item1.First(), waypoints.First());
        Assert.Equal(path.Value.Item1.Last(), waypoints.Last());
    }

    [Fact]
    public void WaypointPath_RefuelPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("origin", "", "", 0, 0, null, "", null, null, null, null, false, null),
            new("refuel", "", "", 0, 100, null, "", null, null, new Marketplace("refuel", null, null, new List<Exchange>() { new Exchange("FUEL", "FUEL", "FUEL") }, null, null), null, false, null),
            new("destination", "", "", 0, 200, null, "", null, null, null, null, false, null),
        };
        var paths = PathsService.BuildWaypointPath(waypoints, waypoints.First(), 150, 150);
        var path = paths.SingleOrDefault(p => p.Key.Symbol == waypoints.Last().Symbol);
        Assert.NotEqual(default, path);
        Assert.Equal(waypoints[0].Symbol, path.Value.Item1[0].Symbol);
        Assert.Equal(waypoints[1].Symbol, path.Value.Item1[1].Symbol);
        Assert.Equal(waypoints[2].Symbol, path.Value.Item1[2].Symbol);
        Assert.Equal(waypoints.Count(), path.Value.Item1.Count());

        Assert.NotEqual(default, path);
        Assert.Equal(path.Value.Item1.First(), waypoints.First());
        Assert.Equal(path.Value.Item1.Last(), waypoints.Last());
    }

    [Fact]
    public async Task SystemPath_EasyPath()
    {
        var secondWaypointSymbol = "Z1-ZY1-ZYX1";
        var secondWaypoint = new Waypoint(secondWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", 0, 0, null, "", null, null, null, null, false, null);
        var secondSystem = new STSystem("", WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", "", 0, 0, new List<Waypoint> { secondWaypoint }, null, null);
        var jumpgate = new JumpGate("A1-AB1-ABC1", new List<string> { secondWaypoint.Symbol });

        var firstWaypointSymbol = "A1-AB1-ABC1";
        var firstWaypointCompletedJumpGate = new Waypoint(firstWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(firstWaypointSymbol), "", 0, 0, null, "", null, null, null, jumpgate, false, null);
        var firstSystem = new STSystem("Constellation", "A1-AB1", "A1", "", 0, 0, new List<Waypoint> { firstWaypointCompletedJumpGate }, null, "");

        ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        systemsServiceSub
            .GetAsync()
            .Returns(new List<STSystem> { firstSystem, secondSystem });

        var ship = new Ship("", null, null, null, null, null, null, null, null, null, new Fuel(10, 10, null), null, null, null);

        IPathsService pathsService = new PathsService(systemsServiceSub);
        var systemPath = await pathsService.BuildSystemPath(
            firstWaypointSymbol,
            ship.Fuel.Capacity, ship.Fuel.Current);
        Assert.Equal(2, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item1.Count());
        Assert.Equal(firstWaypointSymbol, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item1[0].Symbol);
        Assert.Equal(secondWaypointSymbol, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item1[1].Symbol);
    }

    [Fact]
    public async Task SystemPath_NoPath()
    {
        var IS_UNDER_CONSTRUCTION = true;

        var secondWaypointSymbol = "Z1-ZY1-ZYX1";
        var secondWaypoint = new Waypoint(secondWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", 0, 0, null, "", null, null, null, null, false, null);
        var secondSystem = new STSystem("", WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", "", 0, 0, new List<Waypoint> { secondWaypoint }, null, null);

        var jumpgate = new JumpGate("A1-Ab1-ABC1", new List<string> { secondWaypoint.Symbol });
        var firstWaypointSymbol = "A1-AB1-ABC1";
        var firstWaypointIncompleteJumpGate = new Waypoint(firstWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(firstWaypointSymbol), "", 0, 0, null, "", null, null, null, jumpgate, IS_UNDER_CONSTRUCTION, null);
        var firstSystem = new STSystem("Constellation", "A1-AB1", "A1", "", 0, 0, new List<Waypoint> { firstWaypointIncompleteJumpGate }, null, "");

        ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        systemsServiceSub
            .GetAsync()
            .Returns(new List<STSystem> { firstSystem, secondSystem });

        var ship = new Ship("", null, null, null, null, null, null, null, null, null, new Fuel(10, 10, null), null, null, null);

        IPathsService pathsService = new PathsService(systemsServiceSub);
        var systemPath = await pathsService.BuildSystemPath(
            firstWaypointSymbol,
            ship.Fuel.Capacity, ship.Fuel.Current);
        Assert.Single(systemPath.Single(p => p.Key.Symbol == firstWaypointSymbol).Value.Item1);
        Assert.Equal(firstWaypointSymbol, systemPath.Single(p => p.Key.Symbol == firstWaypointSymbol).Value.Item1[0].Symbol);
    }

    [Fact]
    public async Task SystemPathWithDrift_SimpleCase()
    {
        var firstWaypointSymbol = "A1-AB1-ABC1";
        var firstWaypoint = new Waypoint(firstWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(firstWaypointSymbol), "", 0, 0, null, "", null, null, null, null, false, null);
        var secondWaypointSymbol = "A1-AB1-ABC2";
        var secondWaypoint = new Waypoint(secondWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", 1, 1, null, "", null, null, null, null, false, null);
        var firstSystem = new STSystem("Constellation", "A1-AB1", "A1", "", 0, 0, new List<Waypoint> { firstWaypoint, secondWaypoint }, null, "");


        ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        systemsServiceSub
            .GetAsync()
            .Returns(new List<STSystem> { firstSystem });

        var ship = new Ship("", null, null, null, null, null, null, null, null, null, new Fuel(10, 10, null), null, null, null);

        IPathsService pathsService = new PathsService(systemsServiceSub);
        var systemPath = await pathsService.BuildSystemPathWithCost(
            firstWaypointSymbol,
            ship.Fuel.Capacity, ship.Fuel.Current);
        Assert.Equal(2, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item1.Count());
        Assert.Equal(firstWaypointSymbol, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item1[0].Symbol);
        Assert.Equal(secondWaypointSymbol, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item1[1].Symbol);
        Assert.Equal(2, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item2);
    }

    [Fact]
    public async Task SystemPathWithDrift_DriftRequired_SingleNavigate()
    {
        var firstWaypointSymbol = "A1-AB1-ABC1";
        var firstWaypoint = new Waypoint(firstWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(firstWaypointSymbol), "", 0, 0, null, "", null, null, null, null, false, null);
        var secondWaypointSymbol = "A1-AB1-ABC2";
        var secondWaypoint = new Waypoint(secondWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", 100, 100, null, "", null, null, null, null, false, null);
        var firstSystem = new STSystem("Constellation", "A1-AB1", "A1", "", 0, 0, new List<Waypoint> { firstWaypoint, secondWaypoint }, null, "");

        ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        systemsServiceSub
            .GetAsync()
            .Returns(new List<STSystem> { firstSystem });

        var ship = new Ship("", null, null, null, null, null, null, null, null, null, new Fuel(10, 10, null), null, null, null);

        IPathsService pathsService = new PathsService(systemsServiceSub);
        var systemPath = await pathsService.BuildSystemPathWithCost(
            firstWaypointSymbol,
            ship.Fuel.Capacity, ship.Fuel.Current);
        Assert.Equal(2, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item1.Count());
        Assert.Equal(firstWaypointSymbol, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item1[0].Symbol);
        Assert.Equal(secondWaypointSymbol, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item1[1].Symbol);
        Assert.Equal(20_164, systemPath.Single(p => p.Key.Symbol == secondWaypointSymbol).Value.Item2);
    }

    [Fact]
    public async Task SystemPathWithDrift_DriftRequired_MultipleNavigate()
    {
        var firstWaypointSymbol = "A1-AB1-ABC1";
        var firstWaypoint = new Waypoint(firstWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(firstWaypointSymbol), "", 0, 0, null, "", null, null, null, null, false, null);
        var secondWaypointSymbol = "A1-AB1-ABC2";
        var secondWaypoint = new Waypoint(secondWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", 100, 100, null, "", null, null, null, null, false, null);
        var thirdWaypointSymbol = "A1-AB1-ABC3";
        var thirdWaypoint = new Waypoint(thirdWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(thirdWaypointSymbol), "", 105, 100, null, "", null, null, null, null, false, null);
        var firstSystem = new STSystem("Constellation", "A1-AB1", "A1", "", 0, 0, new List<Waypoint> { firstWaypoint, secondWaypoint, thirdWaypoint }, null, "");

        ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        systemsServiceSub
            .GetAsync()
            .Returns(new List<STSystem> { firstSystem });

        var ship = new Ship("", null, null, null, null, null, null, null, null, null, new Fuel(10, 10, null), null, null, null);

        IPathsService pathsService = new PathsService(systemsServiceSub);
        var systemPath = await pathsService.BuildSystemPathWithCost(
            firstWaypointSymbol,
            ship.Fuel.Capacity, ship.Fuel.Current);
        Assert.Equal(3, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item1.Count());
        Assert.Equal(firstWaypointSymbol, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item1[0].Symbol);
        Assert.Equal(secondWaypointSymbol, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item1[1].Symbol);
        Assert.Equal(thirdWaypointSymbol, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item1[2].Symbol);
        Assert.Equal(20_169, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item2);
    }

    [Fact]
    public async Task SystemPathWithDrift_DriftRequired_WithJump()
    {

        var thirdWaypointSymbol = "X1-XY1-XYZ1";
        var thirdWaypoint = new Waypoint(thirdWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(thirdWaypointSymbol), "", 105, 100, null, "", null, null, null, null, false, null);
        var secondSystem = new STSystem("Constellation", "X1-XY1", "X1", "", 0, 0, new List<Waypoint> { thirdWaypoint }, null, "");

        var firstWaypointSymbol = "A1-AB1-ABC1";
        var firstWaypoint = new Waypoint(firstWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(firstWaypointSymbol), "", 0, 0, null, "", null, null, null, null, false, null);

        var secondWaypointSymbol = "A1-AB1-ABC2";
        var secondWaypoint = new Waypoint(secondWaypointSymbol, WaypointsService.ExtractSystemFromWaypoint(secondWaypointSymbol), "", 100, 100, null, "", null, null, null, new JumpGate(secondWaypointSymbol, new List<string> { thirdWaypointSymbol }), false, null);
        var firstSystem = new STSystem("Constellation", "A1-AB1", "A1", "", 0, 0, new List<Waypoint> { firstWaypoint, secondWaypoint }, null, "");

        ISystemsService systemsServiceSub = Substitute.For<ISystemsService>();
        systemsServiceSub
            .GetAsync()
            .Returns(new List<STSystem> { firstSystem, secondSystem });

        var ship = new Ship("", null, null, null, null, null, null, null, null, null, new Fuel(10, 10, null), null, null, null);

        IPathsService pathsService = new PathsService(systemsServiceSub);
        var systemPath = await pathsService.BuildSystemPathWithCost(
            firstWaypointSymbol,
            ship.Fuel.Capacity, ship.Fuel.Current);
        Assert.Equal(3, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item1.Count());
        Assert.Equal(firstWaypointSymbol, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item1[0].Symbol);
        Assert.Equal(secondWaypointSymbol, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item1[1].Symbol);
        Assert.Equal(thirdWaypointSymbol, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item1[2].Symbol);
        Assert.Equal(20_164 + PathsService.JUMP_COST, systemPath.Single(p => p.Key.Symbol == thirdWaypointSymbol).Value.Item2);
    }
}
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.