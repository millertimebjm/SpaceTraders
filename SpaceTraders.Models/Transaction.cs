using SpaceTraders.Models.Enum;

namespace SpaceTraders.Models;

// public class MarketTransaction
// {

//     public string WaypointSymbol { get; init; }
//     public string ShipSymbol { get; init; }
//     public string TradeSymbol { get; init; }
//     public TransactionTypeEnum Type { get; init; }
//     public int Units { get; init; }
//     public int PricePerUnit { get; init; }
//     public int TotalPrice { get; init; }
//     public DateTime Timestamp { get; init; }
// }

public record MarketTransaction(
    string WaypointSymbol,
    string ShipSymbol,
    string TradeSymbol,
    TransactionTypeEnum Type,
    int Units,
    int PricePerUnit,
    int TotalPrice,
    DateTime Timestamp
);
// {

//     public string WaypointSymbol { get; init; }
//     public string ShipSymbol { get; init; }
//     public string TradeSymbol { get; init; }
//     public TransactionTypeEnum Type { get; init; }
//     public int Units { get; init; }
//     public int PricePerUnit { get; init; }
//     public int TotalPrice { get; init; }
//     public DateTime Timestamp { get; init; }
// }
