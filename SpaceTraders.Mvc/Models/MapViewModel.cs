using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

public record MapViewModel(
    Task<IReadOnlyList<STSystem>> SystemsTask,
    Task<IEnumerable<ShipStatus>> ShipStatusesTask
);