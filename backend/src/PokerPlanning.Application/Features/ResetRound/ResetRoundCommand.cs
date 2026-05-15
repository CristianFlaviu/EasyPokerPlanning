using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.ResetRound;

public sealed record ResetRoundCommand(
    Guid RoomId,
    Guid CallerParticipantId) : IRequest<Result>;
