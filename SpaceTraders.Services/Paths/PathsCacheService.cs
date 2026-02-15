using System.Text.Json;
using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Paths.Interfaces;

namespace SpaceTraders.Services.Paths;

public class PathsCacheMongoService(IMongoCollectionFactory _collectionFactory) : IPathsCacheService
{
    public async Task<Dictionary<Waypoint, ValueTuple<List<Waypoint>, int>>?> GetSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int startingFuel)
    {
        var key = $"{originWaypoint}-{fuelMax}-{startingFuel}";
        var filter = Builders<SystemPathCacheModel>
            .Filter
            .Eq(w => w.Key, key);

        var collection = _collectionFactory.GetCollection<SystemPathCacheModel>();
        var projection = Builders<SystemPathCacheModel>.Projection.Exclude("_id");

        var systemPathModel = await collection
            .Find(filter)
            .Project<SystemPathCacheModel>(projection)
            .ToListAsync();
        
        if (systemPathModel.Count == 0) return null;
        return systemPathModel.ToDictionary(spm => spm.DestinationWaypoint, spm => (spm.Waypoints, spm.FuelCost));
    }

    public async Task SetSystemPathWithCost(
        string originWaypoint,
        int fuelMax,
        int startingFuel,
        Dictionary<Waypoint, ValueTuple<List<Waypoint>, int>> systemPath)
    {
        var key = $"{originWaypoint}-{fuelMax}-{startingFuel}";
        var systemPathList = systemPath
            .Select(sp => new SystemPathCacheModel(key, sp.Key, sp.Value.Item1, sp.Value.Item2))
            .ToList();

        var collection = _collectionFactory.GetCollection<SystemPathCacheModel>();
        await collection.InsertManyAsync(systemPathList);
    }
}

public record SystemPathCacheModel(string Key, Waypoint DestinationWaypoint, List<Waypoint> Waypoints, int FuelCost) {}