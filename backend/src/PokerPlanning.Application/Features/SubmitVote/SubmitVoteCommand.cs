using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.SubmitVote;

public sealed record SubmitVoteCommand(
    Guid RoomId,
    Guid ParticipantId,
    string Card) : IRequest<Result>;
