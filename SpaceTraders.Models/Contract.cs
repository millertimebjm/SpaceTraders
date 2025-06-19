namespace SpaceTraders.Models;

public record STContract(
    string Id,
    string FactionSymbol,
    string Type,
    Terms Terms,
    bool Accepted,
    bool Fulfilled,
    DateTime DeadlineToAccept
);

public record Deliver(
    string TradeSymbol,
    string DestinationSymbol,
    int UnitsRequired,
    int UnitsFulfilled
);

public record Payment(
    int OnAccepted,
    int OnFulfilled
);

    

public record Terms(
    DateTime Deadline,
    Payment Payment,
    IReadOnlyList<Deliver> Deliver
);

