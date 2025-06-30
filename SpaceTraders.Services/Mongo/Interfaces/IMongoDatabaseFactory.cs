using MongoDB.Bson;
using MongoDB.Driver;

namespace SpaceTraders.Services;

public interface IMongoCollectionFactory
{
    public IMongoCollection<T> GetCollection<T>();
}