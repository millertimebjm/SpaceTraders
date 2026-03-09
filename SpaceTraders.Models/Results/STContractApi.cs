namespace SpaceTraders.Models.Results;

public record STContractApi(
    string FactionSymbol,
    string Type,
    Terms Terms,
    bool Accepted,
    bool Fulfilled,
    DateTime DeadlineToAccept,
    string Id
)
{
    public static STContract MapToSTContract(STContractApi model)
    {
        return new STContract(
            model.FactionSymbol,
            model.Type,
            model.Terms,
            model.Accepted,
            model.Fulfilled,
            model.DeadlineToAccept,
            ContractId: model.Id
        );
    }
};