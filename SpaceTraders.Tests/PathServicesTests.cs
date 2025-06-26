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
            new("origin", "", "", 0, 0, null, "", null, null),
            new("destination", "", "", 400, 400, null, "", null, null),
        };
        var path = PathsService.GetPathAsync(
            waypoints,
            waypoints.First(),
            waypoints.Last(),
            1);
        Assert.Null(path);
    }

    [Fact]
    public void EasyPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("origin", "", "", 0, 0, null, "", null, null),
            new("destination", "", "", 1, 1, null, "", null, null),
        };
        var path = PathsService.GetPathAsync(
            waypoints,
            waypoints.First(),
            waypoints.Last(),
            200);
        Assert.Equal(waypoints.First().Symbol, path.First().Symbol);
        Assert.Equal(waypoints.Last().Symbol, path.Last().Symbol);
        Assert.Equal(waypoints.Count(), path.Count());
    }

    [Fact]
    public void RefuelPath()
    {
        var waypoints = new List<Waypoint>()
        {
            new("origin", "", "", 0, 0, null, "", null, null),
            new("refuel", "", WaypointTypesEnum.FUEL_STATION.ToString(), 0, 100, null, "", null, null),
            new("destination", "", "", 0, 200, null, "", null, null),
        };
        var path = PathsService.GetPathAsync(
            waypoints,
            waypoints.First(),
            waypoints.Last(),
            150)?.ToList();
        Assert.NotNull(path);
        Assert.Equal(waypoints[0].Symbol, path[0].Symbol);
        Assert.Equal(waypoints[1].Symbol, path[1].Symbol);
        Assert.Equal(waypoints[2].Symbol, path[2].Symbol);
        Assert.Equal(waypoints.Count(), path.Count());
    }
}
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.