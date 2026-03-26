using SpaceTraders.Models;
using SpaceTraders.Services.Trades;

public record TradeModelsViewModel(
    Task<IReadOnlyList<TradeModel>> OrderedModelTradesTask, 
    Task<List<PathModelWithBurn>> PathModelsTask,
    string WaypointSymbol);