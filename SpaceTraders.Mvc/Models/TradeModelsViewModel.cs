using SpaceTraders.Models;
using SpaceTraders.Services.Trades;

public record TradeModelsViewModel(
    Task<IReadOnlyList<TradeModel>> OrderedModelTradesTask, 
    Task<List<PathModel>> PathModelsTask,
    string WaypointSymbol);