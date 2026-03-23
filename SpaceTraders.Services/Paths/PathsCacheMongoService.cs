using System.Text.Json;
using MongoDB.Driver;
using SpaceTraders.Models;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Paths.Interfaces;

namespace SpaceTraders.Services.Paths;

public class PathsCacheMongoService(IMongoCollectionFactory _collectionFactory) : IPathsCacheService
{
    public async Task<Dictionary<string, ValueTuple<List<string>, int>>?> GetSystemPathWithCost(
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
        Dictionary<string, ValueTuple<List<string>, int>> systemPath)
    {
        var key = $"{originWaypoint}-{fuelMax}-{startingFuel}";
        var systemPathList = systemPath
            .Select(sp => new SystemPathCacheModel(key, sp.Key, sp.Value.Item1, sp.Value.Item2))
            .ToList();

        var collection = _collectionFactory.GetCollection<SystemPathCacheModel>();
        await collection.InsertManyAsync(systemPathList);
    }

    public async Task ClearAllCachedSystemPaths()
    {
        var filter = FilterDefinition<SystemPathCacheModel>.Empty;

        var collection = _collectionFactory.GetCollection<SystemPathCacheModel>();
        await collection.DeleteManyAsync(filter);
    }

    public async Task<(decimal? NavigationFactor, int? TimeCost)> GetNavigationFactor(string exportSymbol, string importSymbol, int fuelMax, int fuelCurrent)
    {
        var key = $"{exportSymbol}-{importSymbol}-{fuelMax}-{fuelCurrent}";
        var filter = Builders<NavigationFactorModel>
            .Filter
            .Eq(w => w.Key, key);

        var collection = _collectionFactory.GetCollection<NavigationFactorModel>();
        var projection = Builders<NavigationFactorModel>.Projection.Exclude("_id");

        var navigationFactorModel = await collection
            .Find(filter)
            .Project<NavigationFactorModel>(projection)
            .FirstOrDefaultAsync();
        return (navigationFactorModel?.NavigationFactor, navigationFactorModel?.TimeCost);
    }

    public async Task SetNavigationFactor(string exportSymbol, string importSymbol, int fuelMax, int fuelCurrent, decimal navigationFactor, int timeCost)
    {
        var key = $"{exportSymbol}-{importSymbol}-{fuelMax}-{fuelCurrent}";
        var navigationFactorModel = new NavigationFactorModel(key, navigationFactor, timeCost);
        var collection = _collectionFactory.GetCollection<NavigationFactorModel>();
        await collection.InsertOneAsync(navigationFactorModel);
    }

    public async Task<List<PathModelWithBurn>?> GetMemoizeSystemTravel(string key)
    {
        var collection = _collectionFactory.GetCollection<PathModelWithBurnMemoize>();
        var filter = Builders<PathModelWithBurnMemoize>.Filter.Eq(p => p.Key, key);

        var projection = Builders<PathModelWithBurnMemoize>.Projection.Exclude("_id");
        var pathModels = await collection
            .Find(filter)
            .Project<PathModelWithBurnMemoize>(projection)
            .SingleOrDefaultAsync();
        return pathModels?.PathModels;
    }

    public async Task SetMemoizeSystemTravel(string key, List<PathModelWithBurn> systemPath)
    {
        var collection = _collectionFactory.GetCollection<PathModelWithBurnMemoize>();
        var filter = Builders<PathModelWithBurnMemoize>.Filter.Eq(p => p.Key, key);

        var projection = Builders<PathModelWithBurnMemoize>.Projection.Exclude("_id");
        await collection.DeleteOneAsync(filter);
        await collection.InsertOneAsync(new PathModelWithBurnMemoize(key, systemPath));
    }
}

public record SystemPathCacheModel(string Key, string DestinationWaypoint, List<string> Waypoints, int FuelCost) {}

public record NavigationFactorModel(string Key, decimal NavigationFactor, int TimeCost);

public record PathModelWithBurnMemoize(string Key, List<PathModelWithBurn> PathModels);