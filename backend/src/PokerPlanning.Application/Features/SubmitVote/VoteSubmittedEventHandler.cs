using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Common;
using PokerPlanning.Domain.Rooms.Events;

namespace PokerPlanning.Application.Features.SubmitVote;

public sealed class VoteSubmittedEventHandler(IRoomNotifier notifier)
    : INotificationHandler<DomainEventNotification<VoteSubmittedEvent>>
{
    public Task Handle(
        DomainEventNotification<VoteSubmittedEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        return notifier.VoteSubmittedAsync(
            domainEvent.RoomId,
            domainEvent.RoundId,
            domainEvent.ParticipantId,
            cancellationToken);
    }
}
