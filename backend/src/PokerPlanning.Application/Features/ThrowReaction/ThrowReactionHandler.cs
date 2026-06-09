using MediatR;
using PokerPlanning.Application.Abstractions.Realtime;
using PokerPlanning.Application.Abstractions.Security;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;
using PokerPlanning.Domain.Rooms;

namespace PokerPlanning.Application.Features.ThrowReaction;

// Reactions are ephemeral and never persisted, so there is no aggregate to mutate and no
// domain event to raise. The caller's seat is already proven by the access token at the
// endpoint; we only verify the target is a live participant before broadcasting through
// IRoomNotifier (the same notification abstraction the domain-event handlers use).
public sealed class ThrowReactionHandler(
    IRoomAccessAuthorizer access,
    IRoomNotifier notifier)
    : IRequestHandler<ThrowReactionCommand, Result>
{
    public async Task<Result> Handle(ThrowReactionCommand cmd, CancellationToken ct)
    {
        var roomId = new RoomId(cmd.RoomId);
        var to = new ParticipantId(cmd.ToParticipantId);

        if (!await access.IsCurrentParticipantAsync(roomId, to, ct))
            return Result.Failure(RoomErrors.ParticipantNotFound);

        await notifier.ReactionThrownAsync(
            roomId,
            new ParticipantId(cmd.FromParticipantId),
            to,
            cmd.Emoji,
            ct);

        return Result.Success();
    }
}
