using MongoDB.Bson;
using MongoDB.Driver;

namespace SpaceTraders.Services.MongoCache.Interfaces;

public interface IMongoCollectionFactory
{
    public IMongoCollection<T> GetCollection<T>();
}