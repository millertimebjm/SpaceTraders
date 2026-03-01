using MongoDB.Bson;
using MongoDB.Driver;

namespace SpaceTraders.Services.MongoCache.Interfaces;

public interface IMongoCollectionFactory
{
    IMongoCollection<T> GetCollection<T>();
    Task DeleteDatabaseAsync();
    Task<bool> DatabaseExists();
}