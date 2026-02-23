using System.Threading.Channels;
using SpaceTraders.Models;
using SpaceTraders.Services.ShipLogs.Interfaces;

namespace SpaceTraders.Services.ShipLogs;

public class ShipLogsChannelService(
    IShipLogsStorageService _shipLogsStorageService
) : IShipLogsService, IDisposable
{
    public Channel<ShipLog> _logChannel = Channel.CreateUnbounded<ShipLog>();

    public async Task AddAsync(ShipLog shipLog)
    {
        await _logChannel.Writer.WriteAsync(shipLog);
    }

    public void Dispose()
    {
        while (_logChannel.Reader.TryRead(out ShipLog? shipLog))
        {
            if (shipLog is not null)
            {
                _shipLogsStorageService.Set(shipLog);
            }
        }
        GC.SuppressFinalize(this);
    }

    public async Task WriterAsync()
    {
        while (_logChannel.Reader.TryRead(out var shipLog))
        {
            await _shipLogsStorageService.SetAsync(shipLog);
        }
    }
}