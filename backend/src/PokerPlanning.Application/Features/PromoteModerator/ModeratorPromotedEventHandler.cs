using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.PromoteModerator;

public sealed class ModeratorPromotedEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<ModeratorPromotedEvent>>
{
    public Task Handle(
        DomainEventNotification<ModeratorPromotedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        return notifier.ModeratorPromotedAsync(
            domainEvent.RoomId,
            domainEvent.ParticipantId,
            cancellationToken);
    }
}
