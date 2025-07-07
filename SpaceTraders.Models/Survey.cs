namespace SpaceTraders.Models;

public record Deposit(
 string Symbol
    );

    public record Survey(
 string Signature,
 string Symbol,
 IReadOnlyList<Deposit> Deposits,
 DateTime Expiration,
 string Size
    );