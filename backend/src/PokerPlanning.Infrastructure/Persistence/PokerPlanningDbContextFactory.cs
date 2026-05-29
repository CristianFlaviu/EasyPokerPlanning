using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace PokerPlanning.Infrastructure.Persistence;

public sealed class PokerPlanningDbContextFactory : IDesignTimeDbContextFactory<PokerPlanningDbContext>
{
    public PokerPlanningDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PokerPlanningDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=pokerplanning;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "poker"))
            .Options;

        // Design-time only (migrations); domain-event publishing never runs here.
        return new PokerPlanningDbContext(options, new NoopScopeFactory());
    }

    private sealed class NoopScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new NoopScope();

        private sealed class NoopScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new EmptyProvider();

            public void Dispose()
            {
            }
        }

        private sealed class EmptyProvider : IServiceProvider
        {
            public object? GetService(Type serviceType) => null;
        }
    }
}
