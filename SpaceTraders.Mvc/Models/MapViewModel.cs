using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

public record MapViewModel(
    Task<STSystem> SystemTask,
    Task<IEnumerable<ShipStatus>> ShipStatusesTask
);