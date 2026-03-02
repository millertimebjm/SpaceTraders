using SpaceTraders.Models;

namespace SpaceTraders.Services.ShipLogs.Interfaces;

public interface IShipLogsService
{
    Task AddAsync(ShipLog shipLog);
    Task WriterAsync();
    Task<IEnumerable<ShipLog>> GetShipLogsForProfitAnalysisAsync();
}