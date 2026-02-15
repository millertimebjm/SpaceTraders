using Microsoft.EntityFrameworkCore;
using SpaceTraders.Models;
using SpaceTraders.Services.Trades;

namespace SpaceTraders.Services.EntityFrameworkCache;

public class SpaceTraderDbContext : DbContext
{
    public SpaceTraderDbContext(DbContextOptions options) : base(options) {}

    public DbSet<AgentCacheModel> Agents { get; set; }
    public DbSet<ShipStatusCacheModel> ShipStatuses { get; set; }
    public DbSet<SurveyCacheModel> Surveys { get; set; }
    public DbSet<STSystemCacheModel> STSystems { get; set; }
    public DbSet<TradeCacheModel> Trades { get; set; }
    public DbSet<WaypointCacheModel> Waypoints { get; set; }
    public DbSet<TransactionCacheModel> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<AgentCacheModel>()
            .ToTable("Agent")
            .HasKey(a => a.Symbol);
        modelBuilder
            .Entity<ShipStatusCacheModel>()
            .ToTable("ShipStatus")
            .HasKey(ss => ss.Symbol);
        modelBuilder
            .Entity<SurveyCacheModel>()
            .ToTable("Survey")
            .HasKey(s => s.Signature);
        modelBuilder
            .Entity<STSystemCacheModel>()
            .ToTable("STSystem")
            .HasKey(s => s.Symbol);
        modelBuilder
            .Entity<WaypointCacheModel>()
            .ToTable("Waypoint")
            .HasKey(w => w.Symbol);
        modelBuilder
            .Entity<TradeCacheModel>()
            .ToTable("Trade")
            .HasKey(t => t.Id);
        modelBuilder
            .Entity<TransactionCacheModel>()
            .ToTable("STTransaction")
            .HasKey(t => t.Id);
    }
}