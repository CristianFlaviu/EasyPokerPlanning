using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.SubmitFeedback;

public sealed record SubmitFeedbackCommand(
    string? Name,
    string? Email,
    string Message,
    Guid? UserId) : IRequest<Result>;
