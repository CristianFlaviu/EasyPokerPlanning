using MediatR;
using PokerPlanning.Domain.Common;

namespace PokerPlanning.Application.Features.UploadAvatar;

public sealed record UploadAvatarCommand(
    Guid UserId,
    Stream Content,
    string ContentType,
    long Length) : IRequest<Result<UploadAvatarResult>>;

public sealed record UploadAvatarResult(string AvatarUrl);
