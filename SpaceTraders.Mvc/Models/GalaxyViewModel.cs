using SpaceTraders.Models;

namespace SpaceTraders.Mvc.Models;

public record GalaxyViewModel(
    Task<IReadOnlyList<STSystem>> SystemsTask,
    Task<IEnumerable<ShipStatus>> ShipStatusesTask
);