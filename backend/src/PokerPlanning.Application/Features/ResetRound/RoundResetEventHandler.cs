using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.ResetRound;

public sealed class RoundResetEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<RoundResetEvent>>
{
    public Task Handle(
        DomainEventNotification<RoundResetEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        return notifier.RoundResetAsync(
            domainEvent.RoomId,
            domainEvent.RoundId,
            cancellationToken);
    }
}
