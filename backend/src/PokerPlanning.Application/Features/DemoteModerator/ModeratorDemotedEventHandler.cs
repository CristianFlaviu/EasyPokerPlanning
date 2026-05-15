using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.DemoteModerator;

public sealed class ModeratorDemotedEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<ModeratorDemotedEvent>>
{
    public Task Handle(
        DomainEventNotification<ModeratorDemotedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        return notifier.ModeratorDemotedAsync(
            domainEvent.RoomId,
            domainEvent.ParticipantId,
            cancellationToken);
    }
}
