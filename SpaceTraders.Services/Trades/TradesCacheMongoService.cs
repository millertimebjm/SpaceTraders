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
        return await collection.Find(anyFilter).AnyAsync();
    }

    public async Task<bool> AnyTradeModelAsync()
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        var emptyFilter = Builders<TradeModel>.Filter.Empty;
        var projection = Builders<TradeModel>.Projection.Exclude("_id");

        return await collection.Find(emptyFilter)
            .Project<TradeModel>(projection)
            .AnyAsync();
    }

    public async Task UpdateExistingTradeModelsAsync(string waypointSymbol, IReadOnlyList<TradeGood> tradeGoods)
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        var exports = tradeGoods.Where(tg => (tg.Type == TradeGoodTypeEnum.EXPORT.ToString() || tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString()) && tg.Symbol != TradeSymbolsEnum.FUEL.ToString() && tg.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString()).ToList();
        foreach (var tradeGood in exports)
        {
            var filter = Builders<TradeModel>.Filter.Eq(tm => tm.ExportWaypointSymbol, waypointSymbol)
                & Builders<TradeModel>.Filter.Eq(tm => tm.TradeSymbol, tradeGood.Symbol);

            var update = Builders<TradeModel>.Update
                .Set(tm => tm.ExportBuyPrice, tradeGood.PurchasePrice)
                .Set(tm => tm.ExportSupplyEnum, Enum.Parse<SupplyEnum>(tradeGood.Supply));

            await collection.UpdateManyAsync(filter, update);
        }

        var importsExchanges = tradeGoods.Where(tg => (tg.Type == TradeGoodTypeEnum.IMPORT.ToString() || tg.Type == TradeGoodTypeEnum.EXCHANGE.ToString())  && tg.Symbol != TradeSymbolsEnum.FUEL.ToString() && tg.Symbol != TradeSymbolsEnum.ANTIMATTER.ToString());
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

    public async Task InsertNewTradeModelsAsync(List<TradeModel> tradeModels)
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        await collection.InsertManyAsync(tradeModels, new InsertManyOptions(), CancellationToken.None);
    }

    public async Task ReplaceExistingTradeModelsAsync(List<TradeModel> tradeModels)
    {
        var collection = _collectionFactory.GetCollection<TradeModel>();
        foreach (var tradeModel in tradeModels)
        {
            var filter = Builders<TradeModel>.Filter.And(
                Builders<TradeModel>.Filter.Eq(tm => tm.TradeSymbol, tradeModel.TradeSymbol),
                Builders<TradeModel>.Filter.Eq(tm => tm.ExportWaypointSymbol, tradeModel.ExportWaypointSymbol),
                Builders<TradeModel>.Filter.Eq(tm => tm.ImportWaypointSymbol, tradeModel.ImportWaypointSymbol)
            );
            await collection.ReplaceOneAsync(
                filter, 
                tradeModel, 
                new ReplaceOptions { IsUpsert = true }, 
                CancellationToken.None);
        }
    }
}