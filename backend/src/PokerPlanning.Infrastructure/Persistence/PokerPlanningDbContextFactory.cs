using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

        return new PokerPlanningDbContext(options, new NoopPublisher());
    }

    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification =>
            Task.CompletedTask;
    }
}
