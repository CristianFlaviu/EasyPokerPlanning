using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PokerPlanning.Application.Abstractions.Persistence;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;
using PokerPlanning.Domain.Users.Events;

namespace PokerPlanning.Application.Features.UpdateProfile;

// Propagates a user's new display name + avatar to every room they currently participate in.
// Runs in a fresh DI scope: the publishing DbContext still holds the unhandled
// UserProfileUpdatedEvent, so saving on the same scope would re-collect and re-publish it.
public sealed class UserProfileUpdatedEventHandler(IServiceScopeFactory scopeFactory)
    : INotificationHandler<DomainEventNotification<UserProfileUpdatedEvent>>
{
    public async Task Handle(
        DomainEventNotification<UserProfileUpdatedEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        await using var scope = scopeFactory.CreateAsyncScope();
        var rooms = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
        var notifier = scope.ServiceProvider.GetRequiredService<IRoomNotifier>();

        var userRooms = await rooms.ListByParticipantIdAsync(Guid.Empty, e.UserId.Value, ct);

        var affected = new List<(RoomId RoomId, ParticipantId ParticipantId)>();
        foreach (var room in userRooms)
        {
            if (room.UpdateParticipantForUser(e.UserId, e.DisplayName) is { } participantId)
                affected.Add((room.Id, participantId));
        }

        if (affected.Count == 0)
            return;

        await rooms.SaveChangesAsync(ct);

        foreach (var (roomId, participantId) in affected)
            await notifier.ParticipantProfileChangedAsync(roomId, participantId, e.DisplayName, e.AvatarUrl, ct);
    }
}
