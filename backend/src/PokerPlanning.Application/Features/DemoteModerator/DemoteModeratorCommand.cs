using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.DemoteModerator;

public sealed record DemoteModeratorCommand(
    Guid RoomId,
    Guid CallerParticipantId,
    Guid ParticipantId) : IRequest<Result>;
