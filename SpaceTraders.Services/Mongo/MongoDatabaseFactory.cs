using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using SpaceTraders.Models.Enums;

namespace SpaceTraders.Services;

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
        _mongoConnectionString = configuration[ConfigurationEnums.CacheConnectionString.ToString()] ?? "";
        _mongoDatabaseString = configuration[ConfigurationEnums.CacheDatabaseName.ToString()] ?? "";
    }

    public IMongoCollection<T> GetCollection<T>()
    {
        return MongoDatabase.GetCollection<T>(typeof(T).Name);
    }
}