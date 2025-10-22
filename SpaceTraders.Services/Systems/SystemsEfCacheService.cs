using Microsoft.EntityFrameworkCore;
using SpaceTraders.Models;
using SpaceTraders.Services.EfCache;
using SpaceTraders.Services.Systems.Interfaces;

namespace SpaceTraders.Services.Systems;

public class SystemsEfCacheService(SpaceTradersDbContext _context) : ISystemsCacheService
{
    public async Task<IReadOnlyList<STSystem>> GetAsync()
    {
        return await _context.Systems.ToListAsync();
    }

    public async Task<STSystem> GetAsync(string systemSymbol, bool refresh = false)
    {
        return await _context.Systems.FindAsync(systemSymbol);
    }

    public async Task SetAsync(STSystem system)
    {
        _context.Systems.Update(system);
        await _context.SaveChangesAsync();
    }

    public async Task SetAsync(Waypoint waypoint)
    {
        var system = _context.Systems.Find(waypoint.SystemSymbol);
        var waypoints = system.Waypoints.Where(w => w.Symbol != waypoint.Symbol).ToList();
        waypoints.Add(waypoint);
        system = system with { Waypoints = waypoints.OrderBy(w => w.Symbol).ToList() };
    }
}