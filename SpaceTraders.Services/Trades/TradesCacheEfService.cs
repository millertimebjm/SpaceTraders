using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SpaceTraders.Services.EntityFrameworkCache;
using SpaceTraders.Services.Trades.Interfaces;

namespace SpaceTraders.Services.Trades;

// public class TradesCacheEfService(SpaceTraderDbContext _context) : ITradesCacheService
// {
//     public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync()
//     {
//         var tradeModels = await _context.Trades.Select(t => JsonSerializer.Deserialize<TradeModel>(t.TradesJson)).ToListAsync();
//         return tradeModels;
//         // var collection = _collectionFactory.GetCollection<TradeModel>();
//         // var projection = Builders<TradeModel>.Projection.Exclude("_id");

//         // return await collection
//         //     .Find(FilterDefinition<TradeModel>.Empty)
//         //     .Project<TradeModel>(projection)
//         //     .ToListAsync();
//     }

//     public async Task SaveTradeModelsAsync(IReadOnlyList<TradeModel> tradeModels)
//     {
//         await _context.Trades.ExecuteDeleteAsync();
//         var tradeModelsJson = tradeModels.Select(tm => new TradeCacheModel(0, JsonSerializer.Serialize(tm)));
//         await _context.AddRangeAsync(tradeModelsJson);
//         await _context.SaveChangesAsync();
//         // var collection = _collectionFactory.GetCollection<TradeModel>();
//         // await collection.DeleteManyAsync(FilterDefinition<TradeModel>.Empty);
//         // await collection.InsertManyAsync(tradeModels, new InsertManyOptions(), CancellationToken.None);

//     }
// }