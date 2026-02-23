using SpaceTraders.Models;
using SpaceTraders.Services.ShipLogs.Interfaces;

namespace SpaceTraders.Mvc.Models;

public record ShipLogsViewModel(Task<IEnumerable<ShipLog>> ShipLogsTask, ShipLogsFilterModel FilterModel);