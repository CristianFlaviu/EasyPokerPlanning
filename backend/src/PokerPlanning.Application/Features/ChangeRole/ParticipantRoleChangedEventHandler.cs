using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.ChangeRole;

public sealed class ParticipantRoleChangedEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<ParticipantRoleChangedEvent>>
{
    public Task Handle(
        DomainEventNotification<ParticipantRoleChangedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        return notifier.ParticipantRoleChangedAsync(
            domainEvent.RoomId,
            domainEvent.ParticipantId,
            domainEvent.Role,
            cancellationToken);
    }
}
