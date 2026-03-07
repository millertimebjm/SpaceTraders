namespace SpaceTraders.Models;

public record STContract(
    string FactionSymbol,
    string Type,
    Terms Terms,
    bool Accepted,
    bool Fulfilled,
    DateTime DeadlineToAccept
)
{
    public string Id
    {
        get
        {
            return ContractId;
        }
        set
        {
            ContractId = value;
        }
    }
    public string ContractId {get; set;} = "";
}

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

