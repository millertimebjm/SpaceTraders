namespace SpaceTraders.Models;

public record Material(
    string TradeSymbol,
    int Required,
    int Fulfilled
);