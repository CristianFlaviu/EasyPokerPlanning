using MediatR;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.JoinRoom;

public sealed class ParticipantJoinedEventHandler(IRoomNotifier notifier, IUserRepository users)
    : INotificationHandler<DomainEventNotification<ParticipantJoinedEvent>>
{
    public async Task Handle(
        DomainEventNotification<ParticipantJoinedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        string? avatarUrl = null;
        if (domainEvent.UserId is { } userId)
        {
            var user = await users.GetByIdAsync(userId, cancellationToken);
            avatarUrl = user?.AvatarUrl;
        }

        await notifier.ParticipantJoinedAsync(
            domainEvent.RoomId,
            domainEvent.ParticipantId,
            domainEvent.DisplayName,
            domainEvent.Role,
            avatarUrl,
            cancellationToken);
    }
}
