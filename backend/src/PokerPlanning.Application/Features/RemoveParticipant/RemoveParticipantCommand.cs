using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.RemoveParticipant;

public sealed record RemoveParticipantCommand(
    Guid RoomId,
    Guid CallerParticipantId,
    Guid ParticipantId) : IRequest<Result>;
