namespace SpaceTraders.Models;

public record STContract(
    string FactionSymbol,
    string Type,
    Terms Terms,
    bool Accepted,
    bool Fulfilled,
    DateTime DeadlineToAccept,
    string ContractId
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

