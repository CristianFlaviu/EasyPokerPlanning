using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Infrastructure.Persistence;

public sealed class PokerPlanningDbContext(
    DbContextOptions<PokerPlanningDbContext> options,
    IServiceScopeFactory scopeFactory) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EmailLoginToken> EmailLoginTokens => Set<EmailLoginToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("poker");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PokerPlanningDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await PublishDomainEventAsync(domainEvent, cancellationToken);
        }

        foreach (var aggregate in ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity))
        {
            aggregate.ClearDomainEvents();
        }

        return result;
    }

    private async Task PublishDomainEventAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

        // Resolve handlers from a fresh scope: this DbContext may be pooled (its injected
        // services come from the root provider), and some notification handlers depend on
        // scoped services such as IUserRepository. A dedicated scope also avoids re-entrant
        // saves on the context that raised the event.
        using var scope = scopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(notification, ct);
    }
}
