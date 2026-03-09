using System.Diagnostics.Contracts;

namespace SpaceTraders.Models.Results;

public record ContractDeliverResult(STContractApi Contract, Cargo Cargo);