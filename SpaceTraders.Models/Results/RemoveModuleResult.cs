namespace SpaceTraders.Models.Results;

public record RemoveModuleResult(Cargo Cargo, List<Module> Modules, Agent Agent, MarketTransaction Transaction);