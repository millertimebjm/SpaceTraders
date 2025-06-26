using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

// public class ShipViewModel
// {
//     public Task<Ship?> ShipsTask { get; set; } = Task.FromResult<Ship?>(default);
//     public Task<STContract?> ContractTask { get; set; } = Task.FromResult<STContract?>(default);
// }

public record ShipViewModel(
    Task<Ship> ShipTask,
    Task<STContract?> ContractTask
);