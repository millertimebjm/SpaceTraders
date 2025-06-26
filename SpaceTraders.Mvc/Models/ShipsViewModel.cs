using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

// public class ShipsViewModel
// {
//     public Task<IEnumerable<Ship>>? ShipsTask { get; set; }
//     public Task<STContract?>? ContractTask { get; set; }
// }

public record ShipsViewModel(
    Task<IEnumerable<Ship>> ShipsTask,
    Task<STContract?> ContractTask);