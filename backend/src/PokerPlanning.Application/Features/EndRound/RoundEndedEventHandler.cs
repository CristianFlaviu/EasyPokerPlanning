using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.EndRound;

public sealed class RoundEndedEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<RoundEndedEvent>>
{
    public Task Handle(
        DomainEventNotification<RoundEndedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        return notifier.RoundEndedAsync(
            domainEvent.RoomId,
            domainEvent.RoundId,
            domainEvent.FinalEstimate,
            cancellationToken);
    }
}
