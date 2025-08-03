namespace SpaceTraders.Models;

public record PurchaseCargoResult(
    Cargo Cargo,
    Agent Agent,
    MarketTransaction Transaction
);