using System.Diagnostics.Contracts;

namespace SpaceTraders.Models.Results;

public record ContractDeliverResult(STContract Contract, Cargo Cargo);