namespace SpaceTraders.Models.Results;

public record JumpResponse(
    Nav Nav,
    Cooldown Cooldown,
    MarketTransaction Transaction,
    Agent Agent
);