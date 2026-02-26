using Microsoft.Extensions.Logging;
using NSubstitute;
using SpaceTraders.Models;
using SpaceTraders.Services.Systems;
using SpaceTraders.Services.Systems.Interfaces;
using Xunit;

namespace SpaceTraders.Tests.Services;

public class SystemsServiceTests
{
    [Fact]
    public void TraverseTest_SingleSystem()
    {
        const string systemSymbol = "A1-BC1";
        const string jumpGateWaypoint = "A1-BC1-DE1";
        var waypoints = new List<Waypoint>()
        {
            new(jumpGateWaypoint, systemSymbol, "", 0, 0, null, null, null, null, null, null, false, null),
        };
        var systems = new List<STSystem>()
        {
            new("", systemSymbol, "", "", 0, 0, waypoints, null, null),
        };

        var resultSystems = SystemsService.Traverse(systems, systemSymbol);
        Assert.Single(resultSystems);
        Assert.Equal(systemSymbol, resultSystems.Single().Symbol);
    }

    [Fact]
    public void TraverseTest_SingleSystem_InitialSystemUnderConstruction()
    {
        const string systemSymbol = "A1-BC1";
        const string jumpGateWaypoint = "A1-BC1-DE1";
        var waypoints = new List<Waypoint>()
        {
            new(jumpGateWaypoint, systemSymbol, "", 0, 0, null, null, null, null, null, new JumpGate("jumpgate", ["connection"]), true, null),
        };
        var systems = new List<STSystem>()
        {
            new("", systemSymbol, "", "", 0, 0, waypoints, null, null),
        };

        var resultSystems = SystemsService.Traverse(systems, systemSymbol);
        Assert.Single(resultSystems);
        Assert.Equal(systemSymbol, resultSystems.Single().Symbol);
    }

    [Fact]
    public void TraverseTest_SingleSystems_ConnectedSystemUnderConstruction()
    {
        const string systemSymbol = "A1-BC1";
        const string jumpGateWaypoint = $"{systemSymbol}-DE1";
        const string secondSystemSymbol = "X1-YZ1";
        const string secondJumpGateWaypoint = $"{secondSystemSymbol}-XYZ1";
        
        var waypoints = new List<Waypoint>()
        {
            new(jumpGateWaypoint, systemSymbol, "", 0, 0, null, null, null, null, null, new JumpGate(jumpGateWaypoint, [secondJumpGateWaypoint]), false, null),
        };

        
        var secondSystemWaypoints = new List<Waypoint>()
        {
            new(secondJumpGateWaypoint, secondSystemSymbol, "", 0, 0, null, null, null, null, null, new JumpGate(secondJumpGateWaypoint, [jumpGateWaypoint]), true, null)
        };

        var systems = new List<STSystem>()
        {
            new("", systemSymbol, "", "", 0, 0, waypoints, null, null),
            new("", secondSystemSymbol, "", "", 0, 0, secondSystemWaypoints, null, null),
        };

        var resultSystems = SystemsService.Traverse(systems, systemSymbol);
        Assert.Single(resultSystems);
        Assert.Equal(systemSymbol, resultSystems.Single().Symbol);
    }

    [Fact]
    public void TraverseTest_TwoSystems()
    {
        const string systemSymbol = "A1-BC1";
        const string jumpGateWaypoint = $"{systemSymbol}-DE1";
        const string secondSystemSymbol = "X1-YZ1";
        const string secondJumpGateWaypoint = $"{secondSystemSymbol}-XYZ1";
        
        var waypoints = new List<Waypoint>()
        {
            new(jumpGateWaypoint, systemSymbol, "", 0, 0, null, null, null, null, null, new JumpGate(jumpGateWaypoint, [secondJumpGateWaypoint]), false, null),
        };
        
        var secondSystemWaypoints = new List<Waypoint>()
        {
            new(secondJumpGateWaypoint, secondSystemSymbol, "", 0, 0, null, null, null, null, null, new JumpGate(secondJumpGateWaypoint, [jumpGateWaypoint]), false, null)
        };

        var systems = new List<STSystem>()
        {
            new("", systemSymbol, "", "", 0, 0, waypoints, null, null),
            new("", secondSystemSymbol, "", "", 0, 0, secondSystemWaypoints, null, null),
        };

        var resultSystems = SystemsService.Traverse(systems, systemSymbol);
        Assert.Equal(2, resultSystems.Count());
        Assert.Contains(systemSymbol, resultSystems.Select(rs => rs.Symbol));
        Assert.Contains(secondSystemSymbol, resultSystems.Select(rs => rs.Symbol));
    }

    [Fact]
    public void TraverseTest_ThreeSystems()
    {
        const string systemSymbol = "A1-BC1";
        const string jumpGateWaypoint = $"{systemSymbol}-DE1";
        const string secondSystemSymbol = "X1-YZ1";
        const string secondJumpGateWaypoint = $"{secondSystemSymbol}-XYZ1";
        const string thirdSystemSymbol = "12-123";
        const string thirdJumpGateWaypoint = $"{thirdSystemSymbol}-1234";

        var waypoints = new List<Waypoint>()
        {
            new(jumpGateWaypoint, systemSymbol, "", 0, 0, null, null, null, null, null, new JumpGate(jumpGateWaypoint, [secondJumpGateWaypoint]), false, null),
        };
        
        var secondSystemWaypoints = new List<Waypoint>()
        {
            new(secondJumpGateWaypoint, secondSystemSymbol, "", 0, 0, null, null, null, null, null, new JumpGate(secondJumpGateWaypoint, [jumpGateWaypoint, thirdJumpGateWaypoint]), false, null)
        };

        var thirdSystemWaypoints = new List<Waypoint>()
        {
            new(thirdJumpGateWaypoint, thirdSystemSymbol, "", 0, 0, null, null, null, null, null, new JumpGate(thirdJumpGateWaypoint, [secondJumpGateWaypoint]), false, null)
        };

        var systems = new List<STSystem>()
        {
            new("", systemSymbol, "", "", 0, 0, waypoints, null, null),
            new("", secondSystemSymbol, "", "", 0, 0, secondSystemWaypoints, null, null),
            new("", thirdSystemSymbol, "", "", 0, 0, thirdSystemWaypoints, null, null),
        };

        var resultSystems = SystemsService.Traverse(systems, systemSymbol);
        Assert.Equal(3, resultSystems.Count());
        Assert.Contains(systemSymbol, resultSystems.Select(rs => rs.Symbol));
        Assert.Contains(secondSystemSymbol, resultSystems.Select(rs => rs.Symbol));
        Assert.Contains(thirdSystemSymbol, resultSystems.Select(rs => rs.Symbol));
    }
}