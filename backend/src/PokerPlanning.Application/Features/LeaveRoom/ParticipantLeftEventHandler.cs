using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.LeaveRoom;

public sealed class ParticipantLeftEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<ParticipantLeftEvent>>
{
    public Task Handle(
        DomainEventNotification<ParticipantLeftEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        return notifier.ParticipantLeftAsync(
            domainEvent.RoomId,
            domainEvent.ParticipantId,
            cancellationToken);
    }
}
