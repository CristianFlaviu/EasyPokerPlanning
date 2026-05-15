using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.EndRound;

public sealed record EndRoundCommand(
    Guid RoomId,
    Guid CallerParticipantId,
    string? FinalEstimate) : IRequest<Result>;
