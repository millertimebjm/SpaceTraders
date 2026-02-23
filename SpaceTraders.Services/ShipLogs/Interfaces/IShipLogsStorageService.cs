using SpaceTraders.Models;
using SpaceTraders.Models.Enums;

namespace SpaceTraders.Services.ShipLogs.Interfaces;

public interface IShipLogsStorageService
{
    Task<IEnumerable<ShipLog>> GetAsync(ShipLogsFilterModel model);
    Task SetAsync(ShipLog shipLog);
    void Set(ShipLog shipLog);
    Task SetAsync(IEnumerable<ShipLog> shipLogs);
}

public record ShipLogsFilterModel(
    string? ShipSymbol = null,
    ShipLogEnum? ShipLogEnum = null,
    int? Take = 100,
    int? Skip = 0,
    ShipLogsFilterSortModel ShipLogsFilterSortModel = ShipLogsFilterSortModel.DateDescending
);

public enum ShipLogsFilterSortModel
{
    DateDescending,
}