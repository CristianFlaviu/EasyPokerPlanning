using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.PromoteModerator;

public sealed record PromoteModeratorCommand(
    Guid RoomId,
    Guid CallerParticipantId,
    Guid ParticipantId) : IRequest<Result>;
