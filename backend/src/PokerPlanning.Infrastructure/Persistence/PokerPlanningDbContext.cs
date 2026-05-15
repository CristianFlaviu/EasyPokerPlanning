using Microsoft.EntityFrameworkCore;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Infrastructure.Persistence;

public sealed class PokerPlanningDbContext(DbContextOptions<PokerPlanningDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("poker");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PokerPlanningDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
