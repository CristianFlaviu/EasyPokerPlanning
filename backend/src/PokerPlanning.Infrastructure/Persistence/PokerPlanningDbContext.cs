using Microsoft.EntityFrameworkCore;
using MediatR;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users;

namespace PokerPlanning.Infrastructure.Persistence;

public sealed class PokerPlanningDbContext(
    DbContextOptions<PokerPlanningDbContext> options,
    IPublisher publisher) : DbContext(options)
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

    private Task PublishDomainEventAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

        return publisher.Publish(notification, ct);
    }
}
