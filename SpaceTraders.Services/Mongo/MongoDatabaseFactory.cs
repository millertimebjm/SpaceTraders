using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using SpaceTraders.Models.Enums;

namespace SpaceTraders.Services;

public class MongoCollectionFactory : IMongoCollectionFactory
{
    public readonly string _mongoConnectionString;
    public readonly string _mongoDatabase;

    public MongoCollectionFactory(IConfiguration configuration)
    {
        _mongoConnectionString = configuration[ConfigurationEnums.CacheConnectionString.ToString()] ?? "";
        _mongoDatabase = configuration[ConfigurationEnums.CacheDatabaseName.ToString()] ?? "";
    }

    public IMongoCollection<T> GetCollection<T>()
    {
        var client = new MongoClient(_mongoConnectionString);
        var database = client.GetDatabase(_mongoDatabase);
        return database.GetCollection<T>(nameof(T));
    }
}