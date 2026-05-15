using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.RevealVotes;

public sealed class VotesRevealedEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<VotesRevealedEvent>>
{
    public Task Handle(
        DomainEventNotification<VotesRevealedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        return notifier.VotesRevealedAsync(
            domainEvent.RoomId,
            domainEvent.RoundId,
            domainEvent.Votes,
            cancellationToken);
    }
}
