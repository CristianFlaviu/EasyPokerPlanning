using MediatR;
using PokerPlanning.Domain.Common;
using PokerPlanning.Domain.Participants;

namespace PokerPlanning.Application.Features.ChangeRole;

public sealed record ChangeRoleCommand(
    Guid RoomId,
    Guid ParticipantId,
    ParticipantRole Role) : IRequest<Result>;
