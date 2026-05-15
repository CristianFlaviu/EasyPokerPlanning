using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.StartRound;

public sealed record StartRoundCommand(
    Guid RoomId,
    Guid CallerParticipantId,
    string? Title) : IRequest<Result<StartRoundResult>>;

public sealed record StartRoundResult(Guid RoomId, Guid RoundId);
