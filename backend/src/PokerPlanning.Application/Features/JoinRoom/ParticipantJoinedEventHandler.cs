using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.JoinRoom;

public sealed class ParticipantJoinedEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<ParticipantJoinedEvent>>
{
    public async Task Handle(
        DomainEventNotification<ParticipantJoinedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        await notifier.ParticipantJoinedAsync(
            domainEvent.RoomId,
            domainEvent.ParticipantId,
            domainEvent.DisplayName,
            domainEvent.Role,
            cancellationToken);
    }
}
