using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

public record ShipTransactionsModel(
    Ship Ship,
    IReadOnlyList<MarketTransaction> Transactions
);