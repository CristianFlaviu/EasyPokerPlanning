using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.RevealVotes;

public sealed record RevealVotesCommand(
    Guid RoomId,
    Guid CallerParticipantId) : IRequest<Result>;
