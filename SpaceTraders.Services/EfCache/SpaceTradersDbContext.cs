using Microsoft.EntityFrameworkCore;
using SpaceTraders.Models;

namespace SpaceTraders.Services.EfCache;

public class SpaceTradersDbContext : DbContext
{
    public DbSet<Waypoint> Waypoints { get; set; }
    public DbSet<ShipStatus> ShipStatuses { get; set; }
    public DbSet<STSystem> Systems { get; set; }
    public DbSet<Survey> Surveys { get; set; }

    public SpaceTradersDbContext(DbContextOptions<SpaceTradersDbContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Waypoint>()
            .HasKey(w => w.Symbol);

        modelBuilder.Entity<STSystem>()
            .HasKey(s => s.Symbol);

        modelBuilder.Entity<Survey>()
            .HasKey(s => s.Symbol);
    }
}