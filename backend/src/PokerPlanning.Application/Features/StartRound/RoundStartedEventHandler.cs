using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.StartRound;

public sealed class RoundStartedEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<RoundStartedEvent>>
{
    public Task Handle(
        DomainEventNotification<RoundStartedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        return notifier.RoundStartedAsync(
            domainEvent.RoomId,
            domainEvent.RoundId,
            domainEvent.Title,
            cancellationToken);
    }
}
