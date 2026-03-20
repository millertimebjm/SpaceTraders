using MongoDB.Driver;
using SpaceTraders.Models.Enums;
using SpaceTraders.Services.MongoCache.Interfaces;
using SpaceTraders.Services.Trades.Interfaces;

namespace SpaceTraders.Services.Trades;

public class TradesCacheMongoService(IMongoCollectionFactory _collectionFactory) : ITradesCacheService
{
    public async Task<IReadOnlyList<TradeModel>> GetTradeModelsAsync()
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        var projection = Builders<TradeModel>.Projection.Exclude("_id");

        return await collection
            .Find(FilterDefinition<TradeModel>.Empty)
            .Project<TradeModel>(projection)
            .ToListAsync();
    }

    public async Task SaveTradeModelsAsync(IReadOnlyList<TradeModel> tradeModels)
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        await collection.DeleteManyAsync(FilterDefinition<TradeModel>.Empty);
        await collection.InsertManyAsync(tradeModels, new InsertManyOptions(), CancellationToken.None);
    }

    public async Task<bool> AnyTradeModelAsync(string waypointSymbol)
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        var anyFilter = Builders<TradeModel>.Filter.Or(
            Builders<TradeModel>.Filter.Eq(tm => tm.ExportWaypointSymbol, waypointSymbol),
            Builders<TradeModel>.Filter.Eq(tm => tm.ImportWaypointSymbol, waypointSymbol)
        );
        var findResult = await collection.FindAsync(anyFilter);
        return await findResult.AnyAsync();
    }

    public async Task<bool> AnyTradeModelAsync()
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        var emptyFilter = Builders<TradeModel>.Filter.Empty;
        var findResult = await collection.FindAsync(emptyFilter);
        return await findResult.AnyAsync();
    }

    public async Task UpdateTradeModelAsync(string waypointSymbol, IReadOnlyList<TradeGood> tradeGoods)
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();

        var exports = tradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.EXPORT.ToString() || tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString()).ToList();
        foreach (var tradeGood in exports)
        {
            var filter = Builders<TradeModel>.Filter.Eq(tm => tm.ExportWaypointSymbol, waypointSymbol)
                & Builders<TradeModel>.Filter.Eq(tm => tm.TradeSymbol, tradeGood.Symbol);

            var update = Builders<TradeModel>.Update
                .Set(tm => tm.ExportBuyPrice, tradeGood.PurchasePrice)
                .Set(tm => tm.ExportSupplyEnum, Enum.Parse<SupplyEnum>(tradeGood.Supply));

            await collection.UpdateManyAsync(filter, update);
        }

        var importsExchanges = tradeGoods.Where(tg => tg.Type == TradeGoodTypeEnum.IMPORT.ToString() || tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString());
        foreach (var tradeGood in importsExchanges)
        {
            var filter = Builders<TradeModel>.Filter.Eq(tm => tm.ImportWaypointSymbol, waypointSymbol)
                & Builders<TradeModel>.Filter.Eq(tm => tm.TradeSymbol, tradeGood.Symbol);

            var update = Builders<TradeModel>.Update
                .Set(tm => tm.ImportSellPrice, tradeGood.SellPrice)
                .Set(tm => tm.ImportSupplyEnum, Enum.Parse<SupplyEnum>(tradeGood.Supply));

            await collection.UpdateManyAsync(filter, update);
        }
    }
}