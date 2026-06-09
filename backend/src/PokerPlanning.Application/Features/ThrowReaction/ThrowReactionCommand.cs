using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.ThrowReaction;

// FromParticipantId is the caller's seat (resolved from the access token, never the body)
// so reactions cannot be spoofed as coming from someone else.
public sealed record ThrowReactionCommand(
    Guid RoomId,
    Guid FromParticipantId,
    Guid ToParticipantId,
    string Emoji) : IRequest<Result>;
