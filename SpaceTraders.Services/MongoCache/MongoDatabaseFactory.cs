using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.MongoCache.Interfaces;

namespace SpaceTraders.Services.MongoCache;

public class MongoCollectionFactory : IMongoCollectionFactory
{
    private IMongoClient? _mongoClient;
    private IMongoClient MongoClient
    {
        get
        {
            if (_mongoClient is null)
            {
                _mongoClient = new MongoClient(_mongoConnectionString);
            }
            return _mongoClient;
        }
    }
    private IMongoDatabase? _mongoDatabase = null;
    private IMongoDatabase MongoDatabase
    {
        get
        {
            if (_mongoDatabase is null)
            {
                _mongoDatabase = MongoClient.GetDatabase(_mongoDatabaseString);

            }
            return _mongoDatabase;
        }
    }
    private readonly string _mongoConnectionString;
    private readonly string _mongoDatabaseString;

    public MongoCollectionFactory(IConfiguration configuration)
    {
        _mongoConnectionString = configuration[$"SpaceTrader:"+ConfigurationEnums.CacheConnectionString.ToString()] ?? "";
        _mongoDatabaseString = configuration[$"SpaceTrader:"+ConfigurationEnums.CacheDatabaseName.ToString()] ?? "";
    }

    public IMongoCollection<T> GetCollection<T>()
    {
        return MongoDatabase.GetCollection<T>(typeof(T).Name);
    }
}