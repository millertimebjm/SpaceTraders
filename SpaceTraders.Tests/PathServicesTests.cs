using SpaceTraders.Models;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.Paths;

namespace SpaceTraders.Tests;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
public class PathsServicesTests
{
    [Fact]
    public void NoPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("origin", "", "", 0, 0, null, "", null, null, null, null, false, null),
            new("destination", "", "", 400, 400, null, "", null, null, null, null, false, null),
        };
        var paths = PathsService.BuildDijkstraPath(waypoints, waypoints.First(), 1, 1);
        var path = paths.SingleOrDefault(p => p.Key.Symbol == waypoints.Last().Symbol);
        Assert.Equal(default, path);
    }

    [Fact]
    public void EasyPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("origin", "", "", 0, 0, null, "", null, null, null, null, false, null),
            new("destination", "", "", 1, 1, null, "", null, null, null, null, false, null),
        };
        var paths = PathsService.BuildDijkstraPath(waypoints, waypoints.First(), 2, 2);
        var path = paths.SingleOrDefault(p => p.Key.Symbol == waypoints.Last().Symbol);
        Assert.NotEqual(default, path);
        Assert.Equal(path.Value.Item1.First(), waypoints.First());
        Assert.Equal(path.Value.Item1.Last(), waypoints.Last());
    }

    [Fact]
    public void RefuelPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("origin", "", "", 0, 0, null, "", null, null, null, null, false, null),
            new("refuel", "", "", 0, 100, null, "", null, null, new Marketplace("refuel", null, null, new List<Exchange>() { new Exchange("FUEL", "FUEL", "FUEL") }, null, null), null, false, null),
            new("destination", "", "", 0, 200, null, "", null, null, null, null, false, null),
        };
        var paths = PathsService.BuildDijkstraPath(waypoints, waypoints.First(), 150, 150);
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
}
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.