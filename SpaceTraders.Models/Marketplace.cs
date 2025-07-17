// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using SpaceTraders.Models;
using SpaceTraders.Models.Enums;

public record Marketplace(
 string Symbol,
 IReadOnlyList<Export> Exports,
 IReadOnlyList<Import> Imports,
 IReadOnlyList<Exchange> Exchange,
 IReadOnlyList<MarketTransaction> Transactions,
 IReadOnlyList<TradeGood>? TradeGoods
    );

    public record Exchange(
 string Symbol,
 string Name,
 string Description
    );

    public record Export(
 string Symbol,
 string Name,
 string Description
    );

    public record Import(
 string Symbol,
 string Name,
 string Description
    );

    public record TradeGood(
 string Symbol,
 string Type,
 int TradeVolume,
 string Supply,
 string Activity,
 int PurchasePrice,
 int SellPrice
    );

    public record Transaction(
 string WaypointSymbol,
 string ShipType,
 int Price,
 string AgentSymbol,
 DateTime Timestamp
    );

