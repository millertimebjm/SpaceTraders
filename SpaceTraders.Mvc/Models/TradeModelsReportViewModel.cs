using SpaceTraders.Models;
using SpaceTraders.Services.Trades;

public record TradeModelsReportViewModel(
    Task<List<TradeModel>> BadTradeModelTask);